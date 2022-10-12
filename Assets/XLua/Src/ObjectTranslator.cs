/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif


namespace XLua
{
    using System;
    using System.Collections;
    using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    class ReferenceEqualsComparer : IEqualityComparer<object>
    {
        public new bool Equals(object o1, object o2)
        {
            return object.ReferenceEquals(o1, o2);
        }
        public int GetHashCode(object obj)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }

#pragma warning disable 414
    public class MonoPInvokeCallbackAttribute : System.Attribute
    {
        private Type type;
        public MonoPInvokeCallbackAttribute(Type t) { type = t; }
    }
#pragma warning restore 414

    public enum LuaTypes
    {
        LUA_TNONE = -1,
        LUA_TNIL = 0,
        LUA_TNUMBER = 3,
        LUA_TSTRING = 4,
        LUA_TBOOLEAN = 1,
        LUA_TTABLE = 5,
        LUA_TFUNCTION = 6,
        LUA_TUSERDATA = 7,
        LUA_TTHREAD = 8,
        LUA_TLIGHTUSERDATA = 2
    }

    public enum LuaGCOptions
    {
        LUA_GCSTOP = 0,
        LUA_GCRESTART = 1,
        LUA_GCCOLLECT = 2,
        LUA_GCCOUNT = 3,
        LUA_GCCOUNTB = 4,
        LUA_GCSTEP = 5,
        LUA_GCSETPAUSE = 6,
        LUA_GCSETSTEPMUL = 7,
    }

    public enum LuaThreadStatus
    {
        LUA_RESUME_ERROR = -1,
        LUA_OK = 0,
        LUA_YIELD = 1,
        LUA_ERRRUN = 2,
        LUA_ERRSYNTAX = 3,
        LUA_ERRMEM = 4,
        LUA_ERRERR = 5,
    }

    sealed class LuaIndexes
    {
        /// <summary>
        /// 通过xlua_get_registry_index最终值是LUA_REGISTRYINDEX = -1001000
        /// </summary>
        public static int LUA_REGISTRYINDEX
        {
            get
            {
                return InternalGlobals.LUA_REGISTRYINDEX;
            }
            set
            {
                InternalGlobals.LUA_REGISTRYINDEX = value;
            }
        }
    }

#if GEN_CODE_MINIMIZE
    public delegate int CSharpWrapper(IntPtr L, int top);
#endif

    public partial class ObjectTranslator
    {
        internal MethodWrapsCache methodWrapsCache;
        internal ObjectCheckers objectCheckers;
        internal ObjectCasters objectCasters;
        /// <summary>
        /// 真正存对象的地方，自增index，这个index就是lua里面的缓存的ud指针的值
        /// </summary>
        internal readonly ObjectPool objects = new ObjectPool();
        /// <summary>
        /// addObject时，非enum，非值类型时添加，基本都是引用类型
        /// </summary>
        internal readonly Dictionary<object, int> reverseMap = new Dictionary<object, int>(new ReferenceEqualsComparer());
        internal LuaEnv luaEnv;
        internal StaticLuaCallbacks metaFunctions;
        internal List<Assembly> assemblies;
        private LuaCSFunction importTypeFunction, loadAssemblyFunction, castFunction;
        //延迟加载
        private readonly Dictionary<Type, Action<RealStatePtr>> delayWrap = new Dictionary<Type, Action<RealStatePtr>>();

        private readonly Dictionary<Type, Func<int, LuaEnv, LuaBase>> interfaceBridgeCreators = new Dictionary<Type, Func<int, LuaEnv, LuaBase>>();

        //无法访问的类，比如声明成internal，可以用其接口、基类的生成代码来访问
        private readonly Dictionary<Type, Type> aliasCfg = new Dictionary<Type, Type>();

        /// <summary>
        /// register函数存入delayWrap
        /// </summary>
        /// <param name="type"></param>
        /// <param name="loader"></param>
        public void DelayWrapLoader(Type type, Action<RealStatePtr> loader)
        {
            delayWrap[type] = loader;
        }

        public void AddInterfaceBridgeCreator(Type type, Func<int, LuaEnv, LuaBase> creator)
        {
            interfaceBridgeCreators.Add(type, creator);
        }

        // 只记录类型有没有加载
        Dictionary<Type, bool> loaded_types = new Dictionary<Type, bool>();
        /// <summary>
        /// 注意：这里创建了wrap类的metatable
        /// 对于没有加载过的类型会去调用类型的注入函数，注入函数就是每个wrap类型中的__Register函数
        /// 之所以叫延迟加载，是因为只有在lua代码中用到的warp类才会被真正执行注入；
        /// 或者在被调用getTypeId时发现没有，才会被调用
        /// </summary>
        /// <param name="L"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool TryDelayWrapLoader(RealStatePtr L, Type type)
        {
            if (loaded_types.ContainsKey(type)) return true;
            loaded_types.Add(type, true);
            // 原注释：先建一个metatable，因为加载过程可能会需要用到。
            // 就是BeginObjectRegister里面为啥能找到那个mt，因为这里创建了。上面原本的注释说明了这一点
            // 这个mt在registry中
            LuaAPI.luaL_newmetatable(L, type.FullName);
            // 这里pop掉创建的table，不过用name可以去registry里面找到
            LuaAPI.lua_pop(L, 1);

            Action<RealStatePtr> loader;
            int top = LuaAPI.lua_gettop(L);
            if (delayWrap.TryGetValue(type, out loader))
            {
                delayWrap.Remove(type);
                // 执行注入函数
                loader(L);
            }
            else
            {
#if !GEN_CODE_MINIMIZE && !ENABLE_IL2CPP && (UNITY_EDITOR || XLUA_GENERAL) && !FORCE_REFLECTION && !NET_STANDARD_2_0
                // 不是枚举、不是delegate，是public
                if (!DelegateBridge.Gen_Flag && !type.IsEnum() && !typeof(Delegate).IsAssignableFrom(type) && Utils.IsPublic(type))
                {
                    // 当不是gen出来的warp后，各种过滤后用IL生成了一个warp类
                    Type wrap = ce.EmitTypeWrap(type);
                    // 执行这个类的__Register函数，说白了还是做lua的注入
                    MethodInfo method = wrap.GetMethod("__Register", BindingFlags.Static | BindingFlags.Public);
                    method.Invoke(null, new object[] { L });
                }
                else
                {
                    // xlua.hotfix热更的类型 
                    Utils.ReflectionWrap(L, type, privateAccessibleFlags.Contains(type));
                }
#else
                Utils.ReflectionWrap(L, type, privateAccessibleFlags.Contains(type));
#endif
#if NOT_GEN_WARNING
                if (!typeof(Delegate).IsAssignableFrom(type))
                {
#if !XLUA_GENERAL
                    UnityEngine.Debug.LogWarning(string.Format("{0} not gen, using reflection instead", type));
#else
                    System.Console.WriteLine(string.Format("Warning: {0} not gen, using reflection instead", type));
#endif
                }
#endif
            }
            // 从这里可以看出来，注入函数完成后应该gettop不变的
            if (top != LuaAPI.lua_gettop(L))
            {
                throw new Exception("top change, before:" + top + ", after:" + LuaAPI.lua_gettop(L));
            }

            // 获取嵌套在当前Type中的类型,如果没有注入，则注入
            foreach (var nested_type in type.GetNestedTypes(BindingFlags.Public))
            {
                // 泛型不做？
                if (nested_type.IsGenericTypeDefinition())
                {
                    continue;
                }
                TryDelayWrapLoader(L, nested_type);
            }

            return true;
        }

        /// <summary>
        /// aliasCfg中保存了无法访问的类，比如声明成internal，可以用其接口、基类的生成代码来访问
        /// 目前看只有System.MonoType这个类进来
        /// </summary>
        /// <param name="type"></param>
        /// <param name="alias"></param>
        public void Alias(Type type, string alias)
        {
            Type alias_type = FindType(alias);
            if (alias_type == null)
            {
                throw new ArgumentException("Can not find " + alias);
            }
            aliasCfg[alias_type] = type;
        }

        public int cacheRef;

        void addAssemblieByName(IEnumerable<Assembly> assemblies_usorted, string name)
        {
            foreach (var assemblie in assemblies_usorted)
            {
                if (assemblie.FullName.StartsWith(name) && !assemblies.Contains(assemblie))
                {
                    assemblies.Add(assemblie);
                    break;
                }
            }
        }

        public ObjectTranslator(LuaEnv luaenv, RealStatePtr L)
        {
#if XLUA_GENERAL  || (UNITY_WSA && !UNITY_EDITOR)
            var dumb_field = typeof(ObjectTranslator).GetField("s_gen_reg_dumb_obj", BindingFlags.Static| BindingFlags.DeclaredOnly | BindingFlags.NonPublic);
            if (dumb_field != null)
            {
                dumb_field.GetValue(null);
            }
#endif
            assemblies = new List<Assembly>();

#if (UNITY_WSA && !ENABLE_IL2CPP) && !UNITY_EDITOR
            var assemblies_usorted = Utils.GetAssemblies();
#else
            assemblies.Add(Assembly.GetExecutingAssembly());
            var assemblies_usorted = AppDomain.CurrentDomain.GetAssemblies();
#endif
            addAssemblieByName(assemblies_usorted, "mscorlib,");
            addAssemblieByName(assemblies_usorted, "System,");
            addAssemblieByName(assemblies_usorted, "System.Core,");
            foreach (Assembly assembly in assemblies_usorted)
            {
                if (!assemblies.Contains(assembly))
                {
                    assemblies.Add(assembly);
                }
            }

            this.luaEnv = luaenv;
            // caster函数集合类，用于把栈上对象转换成cs类型
            objectCasters = new ObjectCasters(this);
            objectCheckers = new ObjectCheckers(this);
            methodWrapsCache = new MethodWrapsCache(this, objectCheckers, objectCasters);
            metaFunctions = new StaticLuaCallbacks();

            // 类型导入函数，非常关键的一个函数，在OpenLib中被注入到xlua["import_type"]中；
            // 在LuaEnv.init_xlua这段代码中被设置在CS table->metatable.__index中调用
            // 以GameObject为例，当lua代码中出现CS.UnityEngine.GameObject时，会走index函数，最后执行"import_type"
            importTypeFunction = new LuaCSFunction(StaticLuaCallbacks.ImportType);
            loadAssemblyFunction = new LuaCSFunction(StaticLuaCallbacks.LoadAssembly);
            castFunction = new LuaCSFunction(StaticLuaCallbacks.Cast);
            // create t , 用来在lua里缓存枚举和非值类型
            LuaAPI.lua_newtable(L);
            // create mt
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "__mode");
            LuaAPI.xlua_pushasciistring(L, "v");
            // mt[__mode] = "v"
            LuaAPI.lua_rawset(L, -3);
            // setmetatable(t, mt) , pop mt
            LuaAPI.lua_setmetatable(L, -2);
            // _G[LUA_REGISTRYINDEX][cacheRef] = t; cacheRef = 3
            cacheRef = LuaAPI.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);

            initCSharpCallLua();
        }

        internal enum LOGLEVEL
        {
            NO,
            INFO,
            WARN,
            ERROR
        }

#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
        Type delegate_birdge_type;

        class CompareByArgRet : IEqualityComparer<MethodInfo>
        {
            public bool Equals(MethodInfo x, MethodInfo y)
            {
                return Utils.IsParamsMatch(x, y);
            }
            public int GetHashCode(MethodInfo method)
            {
                int hc = 0;
                hc += method.ReturnType.GetHashCode();
                foreach (var pi in method.GetParameters())
                {
                    hc += pi.ParameterType.GetHashCode();
                }
                return hc;
            }
        }
#endif

        void initCSharpCallLua()
        {
#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
            delegate_birdge_type = typeof(DelegateBridge);
            if (!DelegateBridge.Gen_Flag)
            {
                List<Type> cs_call_lua = new List<Type>();
                foreach (var type in Utils.GetAllTypes())
                {
                    if (type.IsDefined(typeof(CSharpCallLuaAttribute), false))
                    {
                        cs_call_lua.Add(type);
                    }

                    if (!type.IsAbstract || !type.IsSealed) continue;

                    var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (field.IsDefined(typeof(CSharpCallLuaAttribute), false) && (typeof(IEnumerable<Type>)).IsAssignableFrom(field.FieldType))
                        {
                            cs_call_lua.AddRange(field.GetValue(null) as IEnumerable<Type>);
                        }
                    }

                    var props = type.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < props.Length; i++)
                    {
                        var prop = props[i];
                        if (prop.IsDefined(typeof(CSharpCallLuaAttribute), false) && (typeof(IEnumerable<Type>)).IsAssignableFrom(prop.PropertyType))
                        {
                            cs_call_lua.AddRange(prop.GetValue(null, null) as IEnumerable<Type>);
                        }
                    }
                }
                IEnumerable<IGrouping<MethodInfo, Type>> groups = (from type in cs_call_lua
                                                                   where typeof(Delegate).IsAssignableFrom(type) && type != typeof(Delegate) && type != typeof(MulticastDelegate)
                                                                   where !type.GetMethod("Invoke").GetParameters().Any(paramInfo => paramInfo.ParameterType.IsGenericParameter)
                                                                   select type).GroupBy(t => t.GetMethod("Invoke"), new CompareByArgRet());

                ce.SetGenInterfaces(cs_call_lua.Where(type => type.IsInterface()).ToList());
                delegate_birdge_type = ce.EmitDelegateImpl(groups);
            }
#endif
        }

#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
        CodeEmit ce = new CodeEmit();
#endif
        MethodInfo[] genericAction = null;
        MethodInfo[] genericFunc = null;
        Dictionary<Type, Func<DelegateBridgeBase, Delegate>> delegateCreatorCache
            = new Dictionary<Type, Func<DelegateBridgeBase, Delegate>>();

        Func<DelegateBridgeBase, Delegate> getCreatorUsingGeneric(DelegateBridgeBase bridge, Type delegateType, MethodInfo delegateMethod)
        {
            Func<DelegateBridgeBase, Delegate> genericDelegateCreator = null;

            if (genericAction == null)
            {
                var methods = typeof(DelegateBridge).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
                genericAction = methods.Where(m => m.Name == "Action").OrderBy(m => m.GetParameters().Length).ToArray();
                genericFunc = methods.Where(m => m.Name == "Func").OrderBy(m => m.GetParameters().Length).ToArray();
            }
            if (genericAction.Length != 5 || genericFunc.Length != 5)
            {
                return null;
            }
            var parameters = delegateMethod.GetParameters();
#if !XLUA_GENERAL
            if ((delegateMethod.ReturnType.IsValueType() && delegateMethod.ReturnType != typeof(void)) || parameters.Length > 4)
            {
                genericDelegateCreator = (x) => null;
            }
            else
#endif
            {
                foreach (var pinfo in parameters)
                {
                    if (pinfo.ParameterType.IsValueType() || pinfo.IsOut || pinfo.ParameterType.IsByRef)
                    {
                        genericDelegateCreator = (x) => null;
                        break;
                    }
                }
                if (genericDelegateCreator == null)
                {
                    var typeArgs = parameters.Select(pinfo => pinfo.ParameterType);
                    MethodInfo genericMethodInfo = null;
                    if (delegateMethod.ReturnType == typeof(void))
                    {
                        genericMethodInfo = genericAction[parameters.Length];
                    }
                    else
                    {
                        genericMethodInfo = genericFunc[parameters.Length];
                        typeArgs = typeArgs.Concat(new Type[] { delegateMethod.ReturnType });
                    }
                    if (genericMethodInfo.IsGenericMethodDefinition)
                    {
                        var methodInfo = genericMethodInfo.MakeGenericMethod(typeArgs.ToArray());
                        genericDelegateCreator = (o) =>
#if !UNITY_WSA || UNITY_EDITOR
                            Delegate.CreateDelegate(delegateType, o, methodInfo);
#else
                            methodInfo.CreateDelegate(delegateType, bridge); 
#endif
                    }
                    else
                    {
                        genericDelegateCreator = (o) =>
#if !UNITY_WSA || UNITY_EDITOR
                            Delegate.CreateDelegate(delegateType, o, genericMethodInfo);
#else
                            genericMethodInfo.CreateDelegate(delegateType, o);
#endif
                    }
                }
            }

            return genericDelegateCreator;
        }

        /// <summary>
        /// 生成各种代理对象的地方
        /// </summary>
        /// <param name="bridge"></param>
        /// <param name="delegateType"></param>
        /// <returns></returns>
        Delegate getDelegate(DelegateBridgeBase bridge, Type delegateType)
        {
            // DelegatesGensBridge中实现，本质是得到一个new System.Action(__Gen_Delegate_Imp0);
            Delegate ret = bridge.GetDelegateByType(delegateType);

            if (ret != null)
            {
                return ret;
            }

            if (delegateType == typeof(Delegate) || delegateType == typeof(MulticastDelegate))
            {
                return null;
            }

            Func<DelegateBridgeBase, Delegate> delegateCreator;
            if (!delegateCreatorCache.TryGetValue(delegateType, out delegateCreator))
            {
                // get by parameters
                MethodInfo delegateMethod = delegateType.GetMethod("Invoke");
                var methods = bridge.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsGenericMethodDefinition && (m.Name.StartsWith("__Gen_Delegate_Imp") || m.Name == "Action")).ToArray();
                for (int i = 0; i < methods.Length; i++)
                {
                    if (!methods[i].IsConstructor && Utils.IsParamsMatch(delegateMethod, methods[i]))
                    {
                        var foundMethod = methods[i];
                        delegateCreator = (o) =>
#if !UNITY_WSA || UNITY_EDITOR
                            Delegate.CreateDelegate(delegateType, o, foundMethod);
#else
                            foundMethod.CreateDelegate(delegateType, o); 
#endif
                        break;
                    }
                }

                if (delegateCreator == null)
                {
                    delegateCreator = getCreatorUsingGeneric(bridge, delegateType, delegateMethod);
                }
                delegateCreatorCache.Add(delegateType, delegateCreator);
            }

            ret = delegateCreator(bridge);
            if (ret != null)
            {
                return ret;
            }

            throw new InvalidCastException("This type must add to CSharpCallLua: " + delegateType.GetFriendlyName());
        }
        Dictionary<int, WeakReference> delegate_bridges = new Dictionary<int, WeakReference>();
        /// <summary>
        /// 当lua call cs时，如果参数有function就会调用这个
        /// e.g. System.Action _action = translator.GetDelegate<System.Action>(L, 1);
        /// </summary>
        /// <param name="L"></param>
        /// <param name="delegateType"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        public object CreateDelegateBridge(RealStatePtr L, Type delegateType, int idx)
        {
            /*
             * 总结一下lua function是如何被c#调用的:
             * 1、function放到LUA_REGISTRYINDEX表，然后获取ref
             * 2、利用这个ref值创建一个DelegateBridge对象，本质是个LuaBase，ref保存在LuaBase的luaReference
             * 3、根据C#这边的类型，调用GetDelegateByType函数创建一个对象，比如C#这边是Action代表function，
             *    那么就创建一个Action对象。这个对象会被保存到DelegateBridge中
             * 4、如果这个function只对应C#这边一个类型的对象，那么DelegateBridge中用firstKey记录type（Action），
             *    用firstValue保存这个对象。如果这个function被C#这边不同的地方使用，也就是说C#这边不同的类型对象，
             *    都代表这个function，那么在DelegateBridge中就用一个Dictionary bindTo 来保存这些不同的类型和实例的关系。
             *    需要注意的是，这些实例对象本质都代表了某个__Gen_Delegate_ImpN函数
             * 5、__Gen_Delegate_ImpN这一系列方法的本质是真正调用function的地方，它里面有luaReference。注意在调用Action时，
             *    真实调用的是__Gen_Delegate_Imp0这个方法，而这里面实际用LuaReference从LUA_REGISTRYINDEX表取到function，
             *    然后PCall那个函数。
            */

            // System.Action _action = translator.GetDelegate<System.Action>(L, 1)为例复制function到栈顶
            LuaAPI.lua_pushvalue(L, idx);
            // // 这个function作为key从从LUA_REGISTRYINDEX取值，这个值是function的ref
            LuaAPI.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
            // 拿到了的处理，第一次是拿不到的，真正第一次的执行在下面
            if (!LuaAPI.lua_isnil(L, -1))
            {
                // 鬼知道拿到的数字是啥，可能是-1001000  ×
                // 这个数字是function在LUA_REGISTRYINDEX的ref  √
                int referenced = LuaAPI.xlua_tointeger(L, -1);
                // 栈顶弹出table
                LuaAPI.lua_pop(L, 1);
                // 因为是弱引用，所以要看看还在不，不在的话要重建了
                if (delegate_bridges[referenced].IsAlive)
                {
                    if (delegateType == null)
                    {
                        return delegate_bridges[referenced].Target;
                    }
                    // 得到第一次创建的bridge
                    DelegateBridgeBase exist_bridge = delegate_bridges[referenced].Target as DelegateBridgeBase;
                    Delegate exist_delegate;
                    // 以Action类型取对象，如果有就返回Action对象，
                    // 记住这里的exist_bridge是一个LuaBase对象，很像是它里面的luaReference
                    if (exist_bridge.TryGetDelegate(delegateType, out exist_delegate))
                    {
                        return exist_delegate;
                    }
                    else
                    {
                        // 如果和之前存的类型不一样，那就得创建新类型的对象了，比如之前是Action，现在可以是UnityAction
                        exist_delegate = getDelegate(exist_bridge, delegateType);
                        // exist_bridge.AddDelegate(typeof(UnityAction), new UnityAction(__Gen_Delegate_Imp0));
                        // 第二个的话会开始把数据放到bindTo了，细节看里面的注释
                        exist_bridge.AddDelegate(delegateType, exist_delegate);
                        return exist_delegate;
                    }
                }
            }
            else
            {
                // 栈顶弹出null
                LuaAPI.lua_pop(L, 1);
            }
            // 第一次的处理：

            // function复制到栈顶
            LuaAPI.lua_pushvalue(L, idx);
            // 获得在L G表？？上function的引用，并且弹出function。
            // 这个reference会作为后面生的bridge对象最核心的数据，因为bridge就是一个LuaBase，代表一个lua对象。
            // 应该是靠的这个reference最后在lua那边取得function的。
            int reference = LuaAPI.luaL_ref(L);
            // key就是func
            LuaAPI.lua_pushvalue(L, idx);
            LuaAPI.lua_pushnumber(L, reference);
            // LUA_REGISTRYINDEX[func] = reference
            LuaAPI.lua_rawset(L, LuaIndexes.LUA_REGISTRYINDEX);
            DelegateBridgeBase bridge;
            try
            {
#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
                if (!DelegateBridge.Gen_Flag)
                {
                    bridge = Activator.CreateInstance(delegate_birdge_type, new object[] { reference, luaEnv }) as DelegateBridgeBase;
                }
                else
#endif
                {
                    // 这个bridge本质是个LuaBase，这个refrerence是luaReference
                    bridge = new DelegateBridge(reference, luaEnv);
                }
            }
            catch (Exception e)
            {
                LuaAPI.lua_pushvalue(L, idx);
                LuaAPI.lua_pushnil(L);
                LuaAPI.lua_rawset(L, LuaIndexes.LUA_REGISTRYINDEX);
                LuaAPI.lua_pushnil(L);
                LuaAPI.xlua_rawseti(L, LuaIndexes.LUA_REGISTRYINDEX, reference);
                throw e;
            }
            if (delegateType == null)
            {
                delegate_bridges[reference] = new WeakReference(bridge);
                return bridge;
            }
            try
            {
                // ret是按照类型new对象，比如Action对象
                var ret = getDelegate(bridge, delegateType);
                // 一个function在lua那边可能用来做为多个C#对象callback（这只是一种用法），但是C#这边却要
                // 根据类型创建多个bridge，只不过存的luaReference都是一个
                // 想象如下场景，一个C#类中有三个Action属性，参数都一样。lua里3个变量都给的是同一个function，
                // 第一次赋值的时候，会走到这里，创建bridge对象，firstKey = Action， firstValue = new Action
                // 第二的赋值的时候， LuaAPI.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX)得到了referenced，这里变量命名
                // 很巧妙啊，确实是引用过了。然后一路过去，会把第一次创建的Action对象返回。
                // 那么问题来了，这个被返回的Action是如何和这个lua的function结合的。毕竟那个reference是存在bridges中的，和Action
                // 没关系，这就要去看真正创建Action对象的地方了，也就是GetDelegateByType这个函数，它里面创建的Action都是带参数的，
                // __Gen_Delegate_Imp0, 可以看到这个方法也是动态生成的。
                bridge.AddDelegate(delegateType, ret);
                // 注意了，这里的key是reference
                delegate_bridges[reference] = new WeakReference(bridge);
                return ret;
            }
            catch (Exception e)
            {
                bridge.Dispose();
                throw e;
            }
        }

        public bool AllDelegateBridgeReleased()
        {
            foreach (var kv in delegate_bridges)
            {
                if (kv.Value.IsAlive)
                {
                    return false;
                }
            }
            return true;
        }

        public void ReleaseLuaBase(RealStatePtr L, int reference, bool is_delegate)
        {
            if (is_delegate)
            {
                LuaAPI.xlua_rawgeti(L, LuaIndexes.LUA_REGISTRYINDEX, reference);
                if (LuaAPI.lua_isnil(L, -1))
                {
                    LuaAPI.lua_pop(L, 1);
                }
                else
                {
                    LuaAPI.lua_pushvalue(L, -1);
                    LuaAPI.lua_rawget(L, LuaIndexes.LUA_REGISTRYINDEX);
                    if (LuaAPI.lua_type(L, -1) == LuaTypes.LUA_TNUMBER && LuaAPI.xlua_tointeger(L, -1) == reference) //
                    {
                        //UnityEngine.Debug.LogWarning("release delegate ref = " + luaReference);
                        LuaAPI.lua_pop(L, 1);// pop LUA_REGISTRYINDEX[func]
                        LuaAPI.lua_pushnil(L);
                        LuaAPI.lua_rawset(L, LuaIndexes.LUA_REGISTRYINDEX); // LUA_REGISTRYINDEX[func] = nil
                    }
                    else //another Delegate ref the function before the GC tick
                    {
                        LuaAPI.lua_pop(L, 2); // pop LUA_REGISTRYINDEX[func] & func
                    }
                }

                LuaAPI.lua_unref(L, reference);
                delegate_bridges.Remove(reference);
            }
            else
            {
                LuaAPI.lua_unref(L, reference);
            }
        }

        public object CreateInterfaceBridge(RealStatePtr L, Type interfaceType, int idx)
        {
            Func<int, LuaEnv, LuaBase> creator;

            if (!interfaceBridgeCreators.TryGetValue(interfaceType, out creator))
            {
#if (UNITY_EDITOR || XLUA_GENERAL) && !NET_STANDARD_2_0
                var bridgeType = ce.EmitInterfaceImpl(interfaceType);
                creator = (int reference, LuaEnv luaenv) =>
                {
                    return Activator.CreateInstance(bridgeType, new object[] { reference, luaEnv }) as LuaBase;
                };
                interfaceBridgeCreators.Add(interfaceType, creator);
#else
                throw new InvalidCastException("This type must add to CSharpCallLua: " + interfaceType);
#endif
            }
            LuaAPI.lua_pushvalue(L, idx);
            return creator(LuaAPI.luaL_ref(L), luaEnv);
        }

        int common_array_meta = -1;
        /// <summary>
        /// 给common_array_meta =5 这个table设置tag、gc等函数，还有给了方法表
        /// </summary>
        /// <param name="L"></param>
        public void CreateArrayMetatable(RealStatePtr L)
        {
            Utils.BeginObjectRegister(null, L, this, 0, 0, 1, 0, common_array_meta);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "Length", StaticLuaCallbacks.ArrayLength);
            Utils.EndObjectRegister(null, L, this, null, null,
                 typeof(System.Array), StaticLuaCallbacks.ArrayIndexer, StaticLuaCallbacks.ArrayNewIndexer);
        }

        int common_delegate_meta = -1;
        /// <summary>
        /// common_delegate_meta = 6 这个table设置tag、gc等函数，还有给了方法表
        /// </summary>
        /// <param name="L"></param>
        public void CreateDelegateMetatable(RealStatePtr L)
        {
            Utils.BeginObjectRegister(null, L, this, 3, 0, 0, 0, common_delegate_meta);
            Utils.RegisterFunc(L, Utils.OBJ_META_IDX, "__call", StaticLuaCallbacks.DelegateCall);
            Utils.RegisterFunc(L, Utils.OBJ_META_IDX, "__add", StaticLuaCallbacks.DelegateCombine);
            Utils.RegisterFunc(L, Utils.OBJ_META_IDX, "__sub", StaticLuaCallbacks.DelegateRemove);
            Utils.EndObjectRegister(null, L, this, null, null,
                 typeof(System.MulticastDelegate), null, null);
        }

        int enumerable_pairs_func = -1;

        /// <summary>
        /// 创建迭代器的一个函数对象，并且放到registry
        /// </summary>
        /// <param name="L"></param>
        internal void CreateEnumerablePairs(RealStatePtr L)
        {
            LuaFunction func = luaEnv.DoString(@"
                return function(obj)
                    local isKeyValuePair
                    local function lua_iter(cs_iter, k)
                        if cs_iter:MoveNext() then
                            local current = cs_iter.Current
                            if isKeyValuePair == nil then
                                if type(current) == 'userdata' then
                                    local t = current:GetType()
                                    isKeyValuePair = t.Name == 'KeyValuePair`2' and t.Namespace == 'System.Collections.Generic'
                                 else
                                    isKeyValuePair = false
                                 end
                                 --print(current, isKeyValuePair)
                            end
                            if isKeyValuePair then
                                return current.Key, current.Value
                            else
                                return k + 1, current
                            end
                        end
                    end
                    return lua_iter, obj:GetEnumerator(), -1
                end
            ")[0] as LuaFunction;
            func.push(L);
            enumerable_pairs_func = LuaAPI.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
            func.Dispose();
        }

        /// <summary>
        /// 给xlua放了一堆闭包函数
        /// </summary>
        /// <param name="L"></param>
        public void OpenLib(RealStatePtr L)
        {
            if (0 != LuaAPI.xlua_getglobal(L, "xlua"))
            {
                throw new Exception("call xlua_getglobal fail!" + LuaAPI.lua_tostring(L, -1));
            }
            LuaAPI.xlua_pushasciistring(L, "import_type");
            LuaAPI.lua_pushstdcallcfunction(L, importTypeFunction);
            // xlua[import_type] = cclosure(importTypeFunction)
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "import_generic_type");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.ImportGenericType);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "cast");
            LuaAPI.lua_pushstdcallcfunction(L, castFunction);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "load_assembly");
            LuaAPI.lua_pushstdcallcfunction(L, loadAssemblyFunction);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "access");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.XLuaAccess);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "private_accessible");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.XLuaPrivateAccessible);
            // xlua[private_accessible] = cclosure(XLuaPrivateAccessible)
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "metatable_operation");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.XLuaMetatableOperation);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "tofunction");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.ToFunction);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "get_generic_method");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.GetGenericMethod);
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.xlua_pushasciistring(L, "release");
            LuaAPI.lua_pushstdcallcfunction(L, StaticLuaCallbacks.ReleaseCsObject);
            LuaAPI.lua_rawset(L, -3);
            // pop xlua tabel
            LuaAPI.lua_pop(L, 1);

            LuaAPI.lua_createtable(L, 1, 4); // 4 for __gc, __tostring, __index, __newindex
            // _G[LUA_REGISTRYINDEX][common_array_meta] = t; common_array_meta = 5
            common_array_meta = LuaAPI.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
            LuaAPI.lua_createtable(L, 1, 4); // 4 for __gc, __tostring, __index, __newindex
            // _G[LUA_REGISTRYINDEX][common_delegate_meta] = t; common_delegate_meta = 6
            common_delegate_meta = LuaAPI.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
        }

        /// <summary>
        /// 给LuaCSFunction类型，创建metatable，并存缓存到typeIdMap和lua
        /// </summary>
        /// <param name="L"></param>
        internal void createFunctionMetatable(RealStatePtr L)
        {
            LuaAPI.lua_newtable(L);
            LuaAPI.xlua_pushasciistring(L, "__gc");
            LuaAPI.lua_pushstdcallcfunction(L, metaFunctions.GcMeta);
            // t.__gc = GcMeta
            LuaAPI.lua_rawset(L, -3);
            LuaAPI.lua_pushlightuserdata(L, LuaAPI.xlua_tag());
            LuaAPI.lua_pushnumber(L, 1);
            // t[0] = 1
            LuaAPI.lua_rawset(L, -3);
            // copy t -1
            LuaAPI.lua_pushvalue(L, -1);
            // _G[LUA_REGISTRYINDEX][type_id] = t，这里type_id就是meta_ref
            // 到此lua测缓存完成  type_id = 4
            int type_id = LuaAPI.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
            LuaAPI.lua_pushnumber(L, type_id);
            // t[1] = type_id
            LuaAPI.xlua_rawseti(L, -2, 1);
            // pop t 
            LuaAPI.lua_pop(L, 1);
            // 缓存在typeIdMap
            typeIdMap.Add(typeof(LuaCSFunction), type_id);
        }

        /// <summary>
        /// 在assembly中找类型
        /// </summary>
        /// <param name="className"></param>
        /// <param name="isQualifiedName"></param>
        /// <returns></returns>
        internal Type FindType(string className, bool isQualifiedName = false)
        {
            // 程序集直接找
            foreach (Assembly assembly in assemblies)
            {
                Type klass = assembly.GetType(className);

                if (klass != null)
                {
                    return klass;
                }
            }
            int p1 = className.IndexOf('[');
            if (p1 > 0 && !isQualifiedName)
            {
                string qualified_name = className.Substring(0, p1 + 1);
                string[] generic_params = className.Substring(p1 + 1, className.Length - qualified_name.Length - 1).Split(',');
                for (int i = 0; i < generic_params.Length; i++)
                {
                    Type generic_param = FindType(generic_params[i].Trim());
                    if (generic_param == null)
                    {
                        return null;
                    }
                    if (i != 0)
                    {
                        qualified_name += ", ";
                    }
                    qualified_name = qualified_name + "[" + generic_param.AssemblyQualifiedName + "]";
                }
                qualified_name += "]";
                return FindType(qualified_name, true);
            }
            return null;
        }

        bool hasMethod(Type type, string methodName)
        {
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.Name == methodName)
                {
                    return true;
                }
            }
            return false;
        }

        internal void collectObject(int obj_index_to_collect)
        {
            object o;

            if (objects.TryGetValue(obj_index_to_collect, out o))
            {
                objects.Remove(obj_index_to_collect);

                if (o != null)
                {
                    int obj_index;
                    //lua gc是先把weak table移除后再调用__gc，这期间同一个对象可能再次push到lua，关联到新的index
                    bool is_enum = o.GetType().IsEnum();
                    if ((is_enum ? enumMap.TryGetValue(o, out obj_index) : reverseMap.TryGetValue(o, out obj_index))
                        && obj_index == obj_index_to_collect)
                    {
                        if (is_enum)
                        {
                            enumMap.Remove(o);
                        }
                        else
                        {
                            reverseMap.Remove(o);
                        }
                    }
                }
            }
        }

        int addObject(object obj, bool is_valuetype, bool is_enum)
        {
            int index = objects.Add(obj);
            if (is_enum)
            {
                enumMap[obj] = index;
            }
            else if (!is_valuetype)
            {
                reverseMap[obj] = index;
            }

            return index;
        }

        internal object GetObject(RealStatePtr L, int index)
        {
            return (objectCasters.GetCaster(typeof(object))(L, index, null));
        }

        public Type GetTypeOf(RealStatePtr L, int idx)
        {
            Type type = null;
            int type_id = LuaAPI.xlua_gettypeid(L, idx);
            if (type_id != -1)
            {
                typeMap.TryGetValue(type_id, out type);
            }
            return type;
        }
        /// <summary>
        /// 泛型模式：拿到指定index对象，判断类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="L"></param>
        /// <param name="index">对象在registry上</param>
        /// <returns></returns>
        public bool Assignable<T>(RealStatePtr L, int index)
        {
            return Assignable(L, index, typeof(T));
        }
        /// <summary>
        /// 判断类型
        /// </summary>
        /// <param name="L"></param>
        /// <param name="index"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Assignable(RealStatePtr L, int index, Type type)
        {
            // 显然得是枚举或者引用类型
            if (LuaAPI.lua_type(L, index) == LuaTypes.LUA_TUSERDATA) // 快路径
            {
                int udata = LuaAPI.xlua_tocsobj_safe(L, index);
                object obj;
                if (udata != -1 && objects.TryGetValue(udata, out obj))
                {
                    RawObject rawObject = obj as RawObject;
                    if (rawObject != null)
                    {
                        obj = rawObject.Target;
                    }
                    if (obj == null)
                    {
                        return !type.IsValueType();
                    }
                    return type.IsAssignableFrom(obj.GetType());
                }

                int type_id = LuaAPI.xlua_gettypeid(L, index);
                Type type_of_struct;
                if (type_id != -1 && typeMap.TryGetValue(type_id, out type_of_struct)) // is struct
                {
                    return type.IsAssignableFrom(type_of_struct);
                }
            }

            return objectCheckers.GetChecker(type)(L, index);
        }

        /// <summary>
        /// 根据idx和类型，转换idx上的lua对为CS对象
        /// </summary>
        /// <param name="L"></param>
        /// <param name="index"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object GetObject(RealStatePtr L, int index, Type type)
        {
            // _G的ud是-1
            int udata = LuaAPI.xlua_tocsobj_safe(L, index);
            // 正常来说，lua那边创建的cs对象的ud就是objects的index，可以直接返回
            if (udata != -1)
            {
                object obj = objects.Get(udata);
                RawObject rawObject = obj as RawObject;
                return rawObject == null ? obj : rawObject.Target;
            }
            else
            {
                // 对象是个ud
                if (LuaAPI.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
                {
                    GetCSObject get;
                    // 获得typeid，这个函数只能处理LUA_TUSERDATA类型的对象
                    int type_id = LuaAPI.xlua_gettypeid(L, index);
                    if (type_id != -1 && type_id == decimal_type_id)
                    {
                        decimal d;
                        Get(L, index, out d);
                        return d;
                    }
                    Type type_of_struct;
                    if (type_id != -1 && typeMap.TryGetValue(type_id, out type_of_struct) && type.IsAssignableFrom(type_of_struct) && custom_get_funcs.TryGetValue(type, out get))
                    {
                        return get(L, index);
                    }
                }
                // 最后到ObjectCast里找转换函数，LuaTable和LuaFunction都在里面
                return (objectCasters.GetCaster(type)(L, index, null));
            }
        }

        /// <summary>
        /// 获取一个函数，对指定index上的对象进行类型转换
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="L"></param>
        /// <param name="index"></param>
        /// <param name="v"></param>
        public void Get<T>(RealStatePtr L, int index, out T v)
        {
            Func<RealStatePtr, int, T> get_func;
            // 找到了就转，这里没有LuaTable的转换函数
            if (tryGetGetFuncByType(typeof(T), out get_func))
            {
                v = get_func(L, index);
            }
            else
            {
                // 没有找到的话
                v = (T)GetObject(L, index, typeof(T));
            }
        }

        public void PushByType<T>(RealStatePtr L, T v)
        {
            Action<RealStatePtr, T> push_func;
            if (tryGetPushFuncByType(typeof(T), out push_func))
            {
                push_func(L, v);
            }
            else
            {
                PushAny(L, v);
            }
        }

#if GENERIC_SHARING
        public T GetByType<T>(RealStatePtr L, int index)
        {
            Func<RealStatePtr, int, T> get_func;
            if (tryGetGetFuncByType(typeof(T), out get_func))
            {
                return get_func(L, index);
            }
            else
            {
                return (T)GetObject(L, index, typeof(T));
            }
        }
#endif

        public T[] GetParams<T>(RealStatePtr L, int index)
        {
            T[] ret = new T[Math.Max(LuaAPI.lua_gettop(L) - index + 1, 0)];
            for (int i = 0; i < ret.Length; i++)
            {
                Get(L, index + i, out ret[i]);
            }
            return ret;
        }

        public Array GetParams(RealStatePtr L, int index, Type type) //反射版本
        {
            Array ret = Array.CreateInstance(type, Math.Max(LuaAPI.lua_gettop(L) - index + 1, 0)); //这个函数，长度为0的话，返回null
            for (int i = 0; i < ret.Length; i++)
            {
                ret.SetValue(GetObject(L, index + i, type), i);
            }
            return ret;
        }
#if UNITY_EDITOR || XLUA_GENERAL
        public void PushParams(RealStatePtr L, Array ary)
        {
            if (ary != null)
            {
                for (int i = 0; i < ary.Length; i++)
                {
                    PushAny(L, ary.GetValue(i));
                }
            }
        }
#endif

        public T GetDelegate<T>(RealStatePtr L, int index) where T : class
        {

            if (LuaAPI.lua_isfunction(L, index))
            {
                return CreateDelegateBridge(L, typeof(T), index) as T;
            }
            else if (LuaAPI.lua_type(L, index) == LuaTypes.LUA_TUSERDATA)
            {
                return (T)SafeGetCSObj(L, index);
            }
            else
            {
                return null;
            }
        }

        Dictionary<Type, int> typeIdMap = new Dictionary<Type, int>();

        //only store the type id to type map for struct
        Dictionary<int, Type> typeMap = new Dictionary<int, Type>();

        public int GetTypeId(RealStatePtr L, Type type)
        {
            bool isFirst;
            return getTypeId(L, type, out isFirst);
        }

        /// <summary>
        /// 私有访问的集合，目前看只有当调用xlua[private_accessible]时才会add数据
        /// </summary>
        HashSet<Type> privateAccessibleFlags = new HashSet<Type>();

        public void PrivateAccessible(RealStatePtr L, Type type)
        {
            if (!privateAccessibleFlags.Contains(type)) //未处理
            {
                privateAccessibleFlags.Add(type);
                if (typeIdMap.ContainsKey(type)) //loaded
                {
                    Utils.MakePrivateAccessible(L, type);
                }
            }
        }

        /// <summary>
        /// 获取typeid，也就是lua里的meta_ref。执行完成后-1位置是类型的metatable
        /// </summary>
        /// <param name="L"></param>
        /// <param name="type"></param>
        /// <param name="is_first"></param>
        /// <param name="log_level"></param>
        /// <returns>类型ID</returns>
        internal int getTypeId(RealStatePtr L, Type type, out bool is_first, LOGLEVEL log_level = LOGLEVEL.WARN)
        {
            int type_id;
            is_first = false;
            // 如果没有缓存
            if (!typeIdMap.TryGetValue(type, out type_id)) // no reference
            {
                // 数组的话直接返回了common_array_meta
                // common_array_meta是translator.OpenLib中创建的，就是个空表的ref
                if (type.IsArray)
                {
                    if (common_array_meta == -1) throw new Exception("Fatal Exception! Array Metatable not inited!");
                    return common_array_meta;
                }
                // 判断type是不是MulticastDelegate，是的话返回common_delegate_meta
                if (typeof(MulticastDelegate).IsAssignableFrom(type))
                {
                    if (common_delegate_meta == -1) throw new Exception("Fatal Exception! Delegate Metatable not inited!");
                    // 如果类型没有注入，则会在这里注入到lua
                    TryDelayWrapLoader(L, type);
                    return common_delegate_meta;
                }

                is_first = true;
                Type alias_type = null;
                // 从aliasCfg里面取出比较特别的类型
                aliasCfg.TryGetValue(type, out alias_type);

                // 从registry获取mt
                LuaAPI.luaL_getmetatable(L, alias_type == null ? type.FullName : alias_type.FullName);
                // 找不到mt的话
                if (LuaAPI.lua_isnil(L, -1)) //no meta yet, try to use reflection meta
                {
                    // pop nil
                    LuaAPI.lua_pop(L, 1);
                    // 进行类型注入
                    if (TryDelayWrapLoader(L, alias_type == null ? type : alias_type))
                    {
                        // 成功完成，-1 是"UnityEngine.GameObject"
                        // 获得mt
                        LuaAPI.luaL_getmetatable(L, alias_type == null ? type.FullName : alias_type.FullName);
                    }
                    else
                    {
                        throw new Exception("Fatal: can not load metatable of type:" + type);
                    }
                }

                //循环依赖，自身依赖自己的class，比如有个自身类型的静态readonly对象。
                if (typeIdMap.TryGetValue(type, out type_id))
                {
                    LuaAPI.lua_pop(L, 1);
                }
                else
                {
                    if (type.IsEnum())
                    {
                        LuaAPI.xlua_pushasciistring(L, "__band");
                        LuaAPI.lua_pushstdcallcfunction(L, metaFunctions.EnumAndMeta);
                        LuaAPI.lua_rawset(L, -3);
                        LuaAPI.xlua_pushasciistring(L, "__bor");
                        LuaAPI.lua_pushstdcallcfunction(L, metaFunctions.EnumOrMeta);
                        LuaAPI.lua_rawset(L, -3);
                    }
                    if (typeof(IEnumerable).IsAssignableFrom(type))
                    {
                        LuaAPI.xlua_pushasciistring(L, "__pairs");
                        LuaAPI.lua_getref(L, enumerable_pairs_func);
                        LuaAPI.lua_rawset(L, -3);
                    }

                    // metatable
                    // metatable
                    // "UnityEngine.GameObject"
                    LuaAPI.lua_pushvalue(L, -1);

                    // _G[LUA_REGISTRYINDEX][type_id] = MT
                    type_id = LuaAPI.luaL_ref(L, LuaIndexes.LUA_REGISTRYINDEX);
                    // type_id
                    // metatable
                    // "UnityEngine.GameObject"
                    LuaAPI.lua_pushnumber(L, type_id);

                    // metatable[1] = type_id
                    LuaAPI.xlua_rawseti(L, -2, 1);

                    //Utils.PrintStack(L, type, typeof(UnityEngine.GameObject), 1);

                    // metatable
                    // "UnityEngine.GameObject"
                    LuaAPI.lua_pop(L, 1);
                    // "UnityEngine.GameObject"
                    // 值类型特别处理
                    if (type.IsValueType())
                    {
                        typeMap.Add(type_id, type);
                    }
                    // 生成的类型放到map
                    typeIdMap.Add(type, type_id);
                }
            }
            // 调试用
            //object t = ObjectTranslatorPool.Instance.Find(L).GetObject(L, 1);
            //Type tt = t.GetType();
            return type_id;
        }

        /// <summary>
        /// 压栈一个基元类型
        /// </summary>
        /// <param name="L"></param>
        /// <param name="o"></param>
        void pushPrimitive(RealStatePtr L, object o)
        {
            if (o is sbyte || o is byte || o is short || o is ushort ||
                    o is int)
            {
                int i = Convert.ToInt32(o);
                LuaAPI.xlua_pushinteger(L, i);
            }
            else if (o is uint)
            {
                LuaAPI.xlua_pushuint(L, (uint)o);
            }
            else if (o is float || o is double)
            {
                double d = Convert.ToDouble(o);
                LuaAPI.lua_pushnumber(L, d);
            }
            else if (o is IntPtr)
            {
                LuaAPI.lua_pushlightuserdata(L, (IntPtr)o);
            }
            else if (o is char)
            {
                LuaAPI.xlua_pushinteger(L, (char)o);
            }
            else if (o is long)
            {
                LuaAPI.lua_pushint64(L, Convert.ToInt64(o));
            }
            else if (o is ulong)
            {
                LuaAPI.lua_pushuint64(L, Convert.ToUInt64(o));
            }
            else if (o is bool)
            {
                bool b = (bool)o;
                LuaAPI.lua_pushboolean(L, b);
            }
            else
            {
                throw new Exception("No support type " + o.GetType());
            }
        }
        /// <summary>
        /// 任意object压栈
        /// </summary>
        /// <param name="L"></param>
        /// <param name="o"></param>
        public void PushAny(RealStatePtr L, object o)
        {
            if (o == null)
            {
                LuaAPI.lua_pushnil(L);
                return;
            }

            Type type = o.GetType();
            // 基元类型基本就是直接lua_pushXXX
            if (type.IsPrimitive())
            {
                pushPrimitive(L, o);
            }
            else if (o is string)
            {
                LuaAPI.lua_pushstring(L, o as string);
            }
            else if (type == typeof(byte[]))
            {
                LuaAPI.lua_pushstring(L, o as byte[]);
            }
            else if (o is decimal)
            {
                PushDecimal(L, (decimal)o);
            }
            else if (o is LuaBase)
            {
                ((LuaBase)o).push(L);
            }
            else if (o is LuaCSFunction)
            {
                Push(L, o as LuaCSFunction);
            }
            // 这里处理的是枚举和struct类型的push
            // custom_push_funcs了的函数来自RegisterPushAndGetAndUpdate
            else if (o is ValueType)
            {
                PushCSObject push;
                if (custom_push_funcs.TryGetValue(o.GetType(), out push))
                {
                    push(L, o);
                }
                else
                {
                    Push(L, o);
                }
            }
            else
            {
                Push(L, o);
            }
        }

        Dictionary<object, int> enumMap = new Dictionary<object, int>();

        public int TranslateToEnumToTop(RealStatePtr L, Type type, int idx)
        {
            object res = null;
            LuaTypes lt = (LuaTypes)LuaAPI.lua_type(L, idx);
            if (lt == LuaTypes.LUA_TNUMBER)
            {
                int ival = (int)LuaAPI.lua_tonumber(L, idx);
                res = Enum.ToObject(type, ival);
            }
            else if (lt == LuaTypes.LUA_TSTRING)
            {
                string sflags = LuaAPI.lua_tostring(L, idx);
                res = Enum.Parse(type, sflags);
            }
            else
            {
                return LuaAPI.luaL_error(L, "#1 argument must be a integer or a string");
            }
            PushAny(L, res);
            return 1;
        }

        /// <summary>
        /// push LuaFunc到栈顶
        /// </summary>
        /// <param name="L"></param>
        /// <param name="o"></param>
        public void Push(RealStatePtr L, LuaCSFunction o)
        {
            // 满足静态，且是MonoPInvokeCallbackAttribute则直接包装成cclosure，放到栈顶
            if (Utils.IsStaticPInvokeCSFunction(o))
            {
                LuaAPI.lua_pushstdcallcfunction(L, o);
            }
            else
            {
                // 按照一般的object进行压栈，此时栈顶是对象udata
                Push(L, (object)o);
                // StaticCSFunctionWraper = StaticLuaCallbacks.StaticCSFunction
                // 这里等于是把原来的o包装成了StaticCSFunction，在进入lua后StaticCSFunction会包装成cclosure，放到栈顶
                LuaAPI.lua_pushstdcallcfunction(L, metaFunctions.StaticCSFunctionWraper, 1);
            }
        }

        public void Push(RealStatePtr L, LuaBase o)
        {
            if (o == null)
            {
                LuaAPI.lua_pushnil(L);
            }
            else
            {
                o.push(L);
            }
        }

        /// <summary>
        /// 对象进入CS缓存以及lua的registry，执行完后o代表的udata在栈顶
        /// </summary>
        /// <param name="L"></param>
        /// <param name="o"></param>
        public void Push(RealStatePtr L, object o)
        {
            // 对象空，则push nil
            if (o == null)
            {
                LuaAPI.lua_pushnil(L);
                return;
            }

            // 下面几句初步排除一些类型
            int index = -1;
            //EndObjectRegister中的的type.BaseType()传进来时值为System.RuntimeType
            Type type = o.GetType();
#if !UNITY_WSA || UNITY_EDITOR
            bool is_enum = type.IsEnum; // false
            bool is_valuetype = type.IsValueType; //false
#else
            bool is_enum = type.GetTypeInfo().IsEnum;
            bool is_valuetype = type.GetTypeInfo().IsValueType;
#endif
            // 枚举或者不是值类型就缓存
            bool needcache = !is_valuetype || is_enum;
            // 需要缓存，然后得到idx后，去cacheRef里面查一次
            if (needcache && (is_enum ? enumMap.TryGetValue(o, out index) : reverseMap.TryGetValue(o, out index)))
            {
                // 1代表找到了缓存，所以return了
                if (LuaAPI.xlua_tryget_cachedud(L, index, cacheRef) == 1)
                {
                    return;
                }
                // 原注释。。。很莫名
                //这里实在太经典了，weaktable先删除，然后GC会延迟调用，当index会循环利用的时候，不注释这行将会导致重复释放
                //collectObject(index);
            }

            bool is_first;
            int type_id = getTypeId(L, type, out is_first);

            //如果一个type的定义含本身静态readonly实例时，getTypeId会push一个实例，这时候应该用这个实例
            if (is_first && needcache && (is_enum ? enumMap.TryGetValue(o, out index) : reverseMap.TryGetValue(o, out index)))
            {
                if (LuaAPI.xlua_tryget_cachedud(L, index, cacheRef) == 1)
                {
                    return;
                }
            }

            index = addObject(o, is_valuetype, is_enum);
            // 结束后-1是udata
            LuaAPI.xlua_pushcsobj(L, index, type_id, needcache, cacheRef);
        }

        public void PushObject(RealStatePtr L, object o, int type_id)
        {
            if (o == null)
            {
                LuaAPI.lua_pushnil(L);
                return;
            }

            int index = -1;
            if (reverseMap.TryGetValue(o, out index))
            {
                if (LuaAPI.xlua_tryget_cachedud(L, index, cacheRef) == 1)
                {
                    return;
                }
            }

            index = addObject(o, false, false);

            LuaAPI.xlua_pushcsobj(L, index, type_id, true, cacheRef);
        }

        public void Update(RealStatePtr L, int index, object obj)
        {
            int udata = LuaAPI.xlua_tocsobj_fast(L, index);

            if (udata != -1)
            {
                objects.Replace(udata, obj);
            }
            else
            {
                UpdateCSObject update;
                if (custom_update_funcs.TryGetValue(obj.GetType(), out update))
                {
                    update(L, index, obj);
                }
                else
                {
                    throw new Exception("can not update [" + obj + "]");
                }
            }
        }

        /// <summary>
        /// 获取对象
        /// udata存在正确值时等于得到objects的index
        /// udata -1时找到一个cast方法来对index位置上的对象进行转换
        /// </summary>
        /// <param name="L"></param>
        /// <param name="index"></param>
        /// <param name="udata"></param>
        /// <returns></returns>
        private object getCsObj(RealStatePtr L, int index, int udata)
        {
            object obj;
            if (udata == -1)
            {
                if (LuaAPI.lua_type(L, index) != LuaTypes.LUA_TUSERDATA) return null;

                Type type = GetTypeOf(L, index);
                if (type == typeof(decimal))
                {
                    decimal v;
                    Get(L, index, out v);
                    return v;
                }
                GetCSObject get;
                if (type != null && custom_get_funcs.TryGetValue(type, out get))
                {
                    return get(L, index);
                }
                else
                {
                    return null;
                }
            }
            else if (objects.TryGetValue(udata, out obj))
            {
#if !UNITY_5 && !XLUA_GENERAL && !UNITY_2017 && !UNITY_2017_1_OR_NEWER && !UNITY_2018
                if (obj != null && obj is UnityEngine.Object && ((obj as UnityEngine.Object) == null))
                {
                    //throw new UnityEngine.MissingReferenceException("The object of type '"+ obj.GetType().Name +"' has been destroyed but you are still trying to access it.");
                    return null;
                }
#endif
                return obj;
            }
            return null;
        }

        /// <summary>
        /// 安全的获取栈上对象，本质是严重了一下xlua_tag
        /// </summary>
        /// <param name="L"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal object SafeGetCSObj(RealStatePtr L, int index)
        {
            return getCsObj(L, index, LuaAPI.xlua_tocsobj_safe(L, index));
        }

        /// <summary>
        /// 用idx快速查找
        /// </summary>
        /// <param name="L"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        internal object FastGetCSObj(RealStatePtr L, int index)
        {
            return getCsObj(L, index, LuaAPI.xlua_tocsobj_fast(L, index));
        }

        internal void ReleaseCSObj(RealStatePtr L, int index)
        {
            int udata = LuaAPI.xlua_tocsobj_safe(L, index);
            if (udata != -1)
            {
                object o = objects.Replace(udata, null);
                if (o != null && reverseMap.ContainsKey(o))
                {
                    reverseMap.Remove(o);
                }
            }
        }

        List<LuaCSFunction> fix_cs_functions = new List<LuaCSFunction>();

        internal LuaCSFunction GetFixCSFunction(int index)
        {
            return fix_cs_functions[index];
        }

        internal void PushFixCSFunction(RealStatePtr L, LuaCSFunction func)
        {
            if (func == null)
            {
                LuaAPI.lua_pushnil(L);
            }
            else
            {
                LuaAPI.xlua_pushinteger(L, fix_cs_functions.Count);
                fix_cs_functions.Add(func);
                LuaAPI.lua_pushstdcallcfunction(L, metaFunctions.FixCSFunctionWraper, 1);
            }
        }

#if GEN_CODE_MINIMIZE
        CSharpWrapper[] csharpWrapper = new CSharpWrapper[0];
        int csharpWrapperSize = 0;

        internal int CallCSharpWrapper(RealStatePtr L, int funcidx, int top)
        {
            return csharpWrapper[funcidx](L, top);
        }

        void ensureCSharpWrapperCapacity(int min)
        {
            if (csharpWrapper.Length < min)
            {
                int num = (csharpWrapper.Length == 0) ? 4 : (csharpWrapper.Length * 2);
                if (num > 2146435071)
                {
                    num = 2146435071;
                }
                if (num < min)
                {
                    num = min;
                }

                var array = new CSharpWrapper[num];
                Array.Copy(csharpWrapper, 0, array, 0, csharpWrapper.Length);
                csharpWrapper = array;
            }
        }

        internal void PushCSharpWrapper(RealStatePtr L, CSharpWrapper func)
        {
            if (func == null)
            {
                LuaAPI.lua_pushnil(L);
            }
            else
            {
                LuaAPI.xlua_push_csharp_wrapper(L, csharpWrapperSize);
                ensureCSharpWrapperCapacity(csharpWrapperSize + 1);
                csharpWrapper[csharpWrapperSize++] = func;
            }
        }
#endif
        /// <summary>
        /// 把oldTop之上的栈数据保存起来并返回，然后在栈上清空这些数据
        /// </summary>
        /// <param name="L"></param>
        /// <param name="oldTop">清空后栈上数据个数</param>
        /// <returns></returns>
        internal object[] popValues(RealStatePtr L, int oldTop)
        {
            int newTop = LuaAPI.lua_gettop(L);
            // 说明没有多余数据
            if (oldTop == newTop)
            {
                return null;
            }
            else
            {
                // 存数据，settop可以直接修改栈上数据数量
                ArrayList returnValues = new ArrayList();
                for (int i = oldTop + 1; i <= newTop; i++)
                {
                    returnValues.Add(GetObject(L, i));
                }
                LuaAPI.lua_settop(L, oldTop);
                return returnValues.ToArray();
            }
        }

        /// <summary>
        /// 把oldTop之上的栈数据保存起来并返回，然后在栈上清空这些数据
        /// </summary>
        /// <param name="L"></param>
        /// <param name="oldTop"></param>
        /// <param name="popTypes">传入类型</param>
        /// <returns></returns>
        internal object[] popValues(RealStatePtr L, int oldTop, Type[] popTypes)
        {
            int newTop = LuaAPI.lua_gettop(L);
            if (oldTop == newTop)
            {
                return null;
            }
            else
            {
                int iTypes;
                ArrayList returnValues = new ArrayList();
                if (popTypes[0] == typeof(void))
                    iTypes = 1;
                else
                    iTypes = 0;
                for (int i = oldTop + 1; i <= newTop; i++)
                {
                    // 与上面的popValues差别就在于获取对象时传入了类型，但是从引用上看，调用的地方并没有传入类型
                    returnValues.Add(GetObject(L, i, popTypes[iTypes]));
                    iTypes++;
                }
                LuaAPI.lua_settop(L, oldTop);
                return returnValues.ToArray();
            }
        }

        public delegate void PushCSObject(RealStatePtr L, object obj);
        public delegate object GetCSObject(RealStatePtr L, int idx);
        public delegate void UpdateCSObject(RealStatePtr L, int idx, object obj);

        private Dictionary<Type, PushCSObject> custom_push_funcs = new Dictionary<Type, PushCSObject>();
        private Dictionary<Type, GetCSObject> custom_get_funcs = new Dictionary<Type, GetCSObject>();
        private Dictionary<Type, UpdateCSObject> custom_update_funcs = new Dictionary<Type, UpdateCSObject>();

        /// <summary>
        /// 这里注册的基本都是枚举和结构体的对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="push"></param>
        /// <param name="get"></param>
        /// <param name="update"></param>
        void registerCustomOp(Type type, PushCSObject push, GetCSObject get, UpdateCSObject update)
        {
            if (push != null) custom_push_funcs.Add(type, push);
            if (get != null) custom_get_funcs.Add(type, get);
            if (update != null) custom_update_funcs.Add(type, update);
        }

        public bool HasCustomOp(Type type)
        {
            return custom_push_funcs.ContainsKey(type);
        }

        private Dictionary<Type, Delegate> push_func_with_type = null;

        bool tryGetPushFuncByType<T>(Type type, out T func) where T : class
        {
            if (push_func_with_type == null)
            {
                push_func_with_type = new Dictionary<Type, Delegate>()
                {
                    {typeof(int),  new Action<RealStatePtr, int>(LuaAPI.xlua_pushinteger) },
                    {typeof(double), new Action<RealStatePtr, double>(LuaAPI.lua_pushnumber) },
                    {typeof(string), new Action<RealStatePtr, string>(LuaAPI.lua_pushstring) },
                    {typeof(byte[]), new Action<RealStatePtr, byte[]>(LuaAPI.lua_pushstring) },
                    {typeof(bool), new Action<RealStatePtr, bool>(LuaAPI.lua_pushboolean) },
                    {typeof(long), new Action<RealStatePtr, long>(LuaAPI.lua_pushint64) },
                    {typeof(ulong), new Action<RealStatePtr, ulong>(LuaAPI.lua_pushuint64) },
                    {typeof(IntPtr), new Action<RealStatePtr, IntPtr>(LuaAPI.lua_pushlightuserdata) },
                    {typeof(decimal), new Action<RealStatePtr, decimal>(PushDecimal) },
                    {typeof(byte),  new Action<RealStatePtr, byte>((L, v) => LuaAPI.xlua_pushinteger(L, v)) },
                    {typeof(sbyte),  new Action<RealStatePtr, sbyte>((L, v) => LuaAPI.xlua_pushinteger(L, v)) },
                    {typeof(char),  new Action<RealStatePtr, char>((L, v) => LuaAPI.xlua_pushinteger(L, v)) },
                    {typeof(short),  new Action<RealStatePtr, short>((L, v) => LuaAPI.xlua_pushinteger(L, v)) },
                    {typeof(ushort),  new Action<RealStatePtr, ushort>((L, v) => LuaAPI.xlua_pushinteger(L, v)) },
                    {typeof(uint),  new Action<RealStatePtr, uint>(LuaAPI.xlua_pushuint) },
                    {typeof(float),  new Action<RealStatePtr, float>((L, v) => LuaAPI.lua_pushnumber(L, v)) },
                };
            }

            Delegate obj;
            if (push_func_with_type.TryGetValue(type, out obj))
            {
                func = obj as T;
                return true;
            }
            else
            {
                func = null;
                return false;
            }
        }

        /// <summary>
        /// lua 栈顶对象转具体类型的函数的集合
        /// </summary>
        private Dictionary<Type, Delegate> get_func_with_type = null;

        /// <summary>
        /// 获取一个函数，用来对指定index上的对象进行类型转换。这里只负责找这个函数
        /// </summary>
        /// <typeparam name="T">Func类型</typeparam>
        /// <param name="type">类型</param>
        /// <param name="func">引用的代理函数</param>
        /// <returns></returns>
        bool tryGetGetFuncByType<T>(Type type, out T func) where T : class
        {
            // get_func_with_type如果为空，则给了一些函数
            // 可以看到都是用来把栈顶对象转为具体类型的函数
            // 注意get_func_with_type会在RegisterPushAndGetAndUpdate中被放一些数据，基本都是wrap里面枚举、结构体这些类型的生成代码中做的
            // 目前看没有LuaTable这个类型的转换函数
            if (get_func_with_type == null)
            {
                get_func_with_type = new Dictionary<Type, Delegate>()
                {
                    {typeof(int), new Func<RealStatePtr, int, int>(LuaAPI.xlua_tointeger) },
                    {typeof(double), new Func<RealStatePtr, int, double>(LuaAPI.lua_tonumber) },
                    {typeof(string), new Func<RealStatePtr, int, string>(LuaAPI.lua_tostring) },
                    {typeof(byte[]), new Func<RealStatePtr, int, byte[]>(LuaAPI.lua_tobytes) },
                    {typeof(bool), new Func<RealStatePtr, int, bool>(LuaAPI.lua_toboolean) },
                    {typeof(long), new Func<RealStatePtr, int, long>(LuaAPI.lua_toint64) },
                    {typeof(ulong), new Func<RealStatePtr, int, ulong>(LuaAPI.lua_touint64) },
                    {typeof(IntPtr), new Func<RealStatePtr, int, IntPtr>(LuaAPI.lua_touserdata) },
                    {typeof(decimal), new Func<RealStatePtr, int, decimal>((L, idx) => {
                        decimal ret;
                        Get(L, idx, out ret);
                        return ret;
                    }) },
                    {typeof(byte), new Func<RealStatePtr, int, byte>((L, idx) => (byte)LuaAPI.xlua_tointeger(L, idx) ) },
                    {typeof(sbyte), new Func<RealStatePtr, int, sbyte>((L, idx) => (sbyte)LuaAPI.xlua_tointeger(L, idx) ) },
                    {typeof(char), new Func<RealStatePtr, int, char>((L, idx) => (char)LuaAPI.xlua_tointeger(L, idx) ) },
                    {typeof(short), new Func<RealStatePtr, int, short>((L, idx) => (short)LuaAPI.xlua_tointeger(L, idx) ) },
                    {typeof(ushort), new Func<RealStatePtr, int, ushort>((L, idx) => (ushort)LuaAPI.xlua_tointeger(L, idx) ) },
                    {typeof(uint), new Func<RealStatePtr, int, uint>(LuaAPI.xlua_touint) },
                    {typeof(float), new Func<RealStatePtr, int, float>((L, idx) => (float)LuaAPI.lua_tonumber(L, idx) ) },
                };
            }

            Delegate obj;
            if (get_func_with_type.TryGetValue(type, out obj))
            {
                func = obj as T;
                return true;
            }
            else
            {
                func = null;
                return false;
            }
        }


        public delegate void GetFunc<T>(RealStatePtr L, int idx, out T val);

        /// <summary>
        /// 注册
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="push"></param>
        /// <param name="get"></param>
        /// <param name="update"></param>
        public void RegisterPushAndGetAndUpdate<T>(Action<RealStatePtr, T> push, GetFunc<T> get, Action<RealStatePtr, int, T> update)
        {
            Type type = typeof(T);
            Action<RealStatePtr, T> org_push;
            Func<RealStatePtr, int, T> org_get;
            if (tryGetPushFuncByType(type, out org_push) || tryGetGetFuncByType(type, out org_get))
            {
                throw new InvalidOperationException("push or get of " + type + " has register!");
            }
            push_func_with_type.Add(type, push);
            get_func_with_type.Add(type, new Func<RealStatePtr, int, T>((L, idx) =>
            {
                T ret;
                get(L, idx, out ret);
                return ret;
            }));

            registerCustomOp(type,
                (RealStatePtr L, object obj) =>
                {
                    push(L, (T)obj);
                },
                (RealStatePtr L, int idx) =>
                {
                    T val;
                    get(L, idx, out val);
                    return val;
                },
                (RealStatePtr L, int idx, object obj) =>
                {
                    update(L, idx, (T)obj);
                }
            );
        }

        public void RegisterCaster<T>(GetFunc<T> get)
        {
            objectCasters.AddCaster(typeof(T), (L, idx, o) =>
            {
                T obj;
                get(L, idx, out obj);
                return obj;
            });
        }

        int decimal_type_id = -1;

        public void PushDecimal(RealStatePtr L, decimal val)
        {
            if (decimal_type_id == -1)
            {
                bool is_first;
                decimal_type_id = getTypeId(L, typeof(decimal), out is_first);
            }
            IntPtr buff = LuaAPI.xlua_pushstruct(L, 16, decimal_type_id);
            if (!CopyByValue.Pack(buff, 0, val))
            {
                throw new Exception("pack fail for decimal ,value=" + val);
            }

        }

        public bool IsDecimal(RealStatePtr L, int index)
        {
            if (decimal_type_id == -1) return false;
            return LuaAPI.xlua_gettypeid(L, index) == decimal_type_id;
        }

        public decimal GetDecimal(RealStatePtr L, int index)
        {
            decimal ret;
            Get(L, index, out ret);
            return ret;
        }

        public void Get(RealStatePtr L, int index, out decimal val)
        {
            LuaTypes lua_type = LuaAPI.lua_type(L, index);
            if (lua_type == LuaTypes.LUA_TUSERDATA)
            {
                if (LuaAPI.xlua_gettypeid(L, index) != decimal_type_id)
                {
                    throw new Exception("invalid userdata for decimal!");
                }

                IntPtr buff = LuaAPI.lua_touserdata(L, index);

                if (!CopyByValue.UnPack(buff, 0, out val))
                {
                    throw new Exception("unpack decimal fail!");
                }
            }
            else if (lua_type == LuaTypes.LUA_TNUMBER)
            {
                if (LuaAPI.lua_isint64(L, index))
                {
                    val = (decimal)LuaAPI.lua_toint64(L, index);
                }
                else
                {
                    val = (decimal)LuaAPI.lua_tonumber(L, index); // has gc
                }
            }
            else
            {
                throw new Exception("invalid lua value for decimal, LuaType=" + lua_type);
            }
        }
    }
}