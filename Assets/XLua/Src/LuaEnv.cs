/*
 * Tencent is pleased to support the open source community by making xLua available.
 * Copyright (C) 2016 THL A29 Limited, a Tencent company. All rights reserved.
 * Licensed under the MIT License (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at
 * http://opensource.org/licenses/MIT
 * Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.
*/

/*
 * 注意：注释中所有的堆栈结果，或者测试结果都是基于06_Coroutine这个项目的
 * 
 * local gameobject = CS.UnityEngine.GameObject('Coroutine_Runner')背后的逻辑
 * 1、触发来自init_xlua中定义的CS.__index，最终调用import_type(fqn)，fqn="UnityEngine.GameObject"
 * 2、import_type的定义来自ObjectTranslator.OpenLib，它对应了StaticLuaCallbacks.ImportType，这里会把类型名称转为真正的Type
 * 3、ImportType->ObjectTranslator.GetTypeId->ObjectTranslator.getTypeId
 * 4、getTypeId中会根据类型的找有没有缓存，一开始肯定是没有的，会执行到TryDelayWrapLoader，最终执行到从delayWrap中获取loader函数，
 *    这里的loader就是wrap类中的__Register函数。
 *    4.1、ObjectTranslator的两个partial class XLuaGenAutoRegister和WarpPusher中利用static变量在类调用时构建的机制，在第一次调用ObjectTranslator时
 *         调用了XLua.LuaEnv.AddIniter(Init)，Init中是调用DelayWrapLoader将__Register函数放入delayWrap
 * 5、__Register最终产生的结果是：
 *         MT[0] = 1
 *         MT[1] = type_id
 *         MT._index = cclosure obj_indexer
 *         MT._newindex = cclosure obj_newindexer
 *         _G[LUA_REGISTRYINDEX][type_id] = MT
 *         CS["UnityEngine"]["GameObject"] = static funcs table
 *         CS[udata] = static funcs table
 *    到此为止import_type(fqn)的功能结束
 * 6、在__index方法中最终返回的是return rawget(self, key) , self是CS["UnityEngine"]，key是"GameObject"。因此CS.UnityEngine.GameObject这句代码返回的是一个包含
 *    了UnityEngine.GameObject静态方法，和__call方法的table。
 * 7、CS.UnityEngine.GameObject('Coroutine_Runner')会调用__call，最终调用到UnityEngineGameObjectWrap.__CreateInstance。
 *    最终调用了 var gen_ret = new UnityEngine.GameObject(_name);
 *              translator.Push(L, gen_ret);
 * 8、translator.Push中对于UnityEngine.GameObject来说是需要needcache的，且第一次在enumMap、reverseMap中都没有cache，因此会进入addObject。
 * 9、addObject中objects.Add会把所有lua代码产生的对象存入到ObjectPool中，这里面真正存储对象的是一个Slot数组，add函数返回的是对象在数组的index。
 * 10、LuaAPI.xlua_pushcsobj做的事情是把CS这边的对象，包装成一个lua测的udata。在xlua.c中可以找到xlua_pushcsobj
 * 11、可以看到这个udata指向一个int*，存的值就是index。对于需要缓存的对象，最终调用_G[LUA_REGISTRYINDEX][cache_ref][key] = udata
 *     这里的cacheRef是ObjectTranslator创建时放到registry中的一个table的ref。
 * 12、最后需要调用下面的代码，这里meta_ref就是CS中的type_id
 *          lua_rawgeti(L, LUA_REGISTRYINDEX, meta_ref); 上面已经知道_G[LUA_REGISTRYINDEX][type_id] = MT，所以得到类型的MT
 *          setmetatable(ud,-1) = setmetatable(ud, metatable)
 *     于是乎这个udata就作为返回值给了local gameobject变量，当出现gameobject.XXX时，就使用了UnityEngine.GameObject的metatable。
 *     而这个metatable就会去调用那些从wrap中注入的C#函数了。            
 */

#if USE_UNI_LUA
*using LuaAPI = UniLua.Lua;
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
    using System.Collections.Generic;

    public class LuaEnv : IDisposable
    {
        /// <summary>
        /// 用于从registry获取init_xlua中创建的CS
        /// </summary>
        public const string CSHARP_NAMESPACE = "xlua_csharp_namespace";
        public const string MAIN_SHREAD = "xlua_main_thread";

        internal RealStatePtr rawL;

        internal RealStatePtr L
        {
            get
            {
                if (rawL == RealStatePtr.Zero)
                {
                    throw new InvalidOperationException("this lua env had disposed!");
                }
                return rawL;
            }
        }

        private LuaTable _G;

        internal ObjectTranslator translator;
        /// <summary>
        /// 错误处理函数
        /// </summary>
        internal int errorFuncRef = -1;

#if THREAD_SAFE || HOTFIX_ENABLE
        internal /*static*/ object luaLock = new object();

        internal object luaEnvLock
        {
            get
            {
                return luaLock;
            }
        }
#endif

        const int LIB_VERSION_EXPECT = 105;

        public LuaEnv()
        {
            /*
             * 发生了什么：
             * 创建lua虚拟机
             * 创建xlua table，_G[xlua]                                1
             * _G[uint64]                                              2
             * 创建translator对象
             * 创建_G[LUA_REGISTRYINDEX][cacheRef] table                3
             * 缓存了 LuaCSFunction的metatable                          4
             * _G["print"]
             * _G[template]
             * package[searchers]加入4个函数，并修改顺序
             * 创建CS table，并定义metatable
             * _G[LUA_REGISTRYINDEX][LuaIndexs] = t
             * _G[LUA_REGISTRYINDEX][LuaNewIndexs] = t
             * _G[LUA_REGISTRYINDEX][LuaClassIndexs] = t
             * _G[LUA_REGISTRYINDEX][LuaClassNewIndexs] = t
             * _G[LUA_REGISTRYINDEX][xlua_main_thread] = 被改造的top
             * _G[LUA_REGISTRYINDEX][xlua_csharp_namespace] = CS
             * translator.aliasCfg  -- 不是那么重要
             * 创造了一个_G的LuaTable，ObjectTranslator.get_func_with_type填充了数据
             * translator.delayWrap中添加gen出来的类的__Register
             * 给common_array_meta表添加方法
             * 给common_delegate_meta表添加方法
             * 给迭代器创建方法，enumerable_pairs_func
             */

            // xlua.c里面写死的
            if (LuaAPI.xlua_get_lib_version() != LIB_VERSION_EXPECT)
            {
                throw new InvalidProgramException("wrong lib version expect:"
                    + LIB_VERSION_EXPECT + " but got:" + LuaAPI.xlua_get_lib_version());
            }

#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            // 注意这里将LUA_REGISTRYINDEX的值真正的赋予了LUA_REGISTRYINDEX的值
            LuaIndexes.LUA_REGISTRYINDEX = LuaAPI.xlua_get_registry_index();
#if GEN_CODE_MINIMIZE
                LuaAPI.xlua_set_csharp_wrapper_caller(InternalGlobals.CSharpWrapperCallerPtr);
#endif
            // Create State
            rawL = LuaAPI.luaL_newstate();

            /*
            LUA_API void luaopen_xlua(lua_State *L) {
              // 最后的结果是LOADED[modname] = module和_G[modname] = module，module是函数执行的结果
                                // 本质是扔到G一堆函数
                                luaL_openlibs(L);

                            #if LUA_VERSION_NUM >= 503
              // 创建一个表，把xlualib这组函数扔进去
                                luaL_newlib(L, xlualib);
                                // 上面创建的表放到_G表中，名为xlua。此时gettop = 0
                                lua_setglobal(L, "xlua");
                            #else
                                luaL_register(L, "xlua", xlualib);
                                lua_pop(L, 1);
                            #endif
                            */
            LuaAPI.luaopen_xlua(rawL); 
            // _G[uint64]里一堆函数
            LuaAPI.luaopen_i64lib(rawL);
            // 创建ObjectTranslator，这个对象持有lua层创建的CS对象，以index（userdata）的形式关联
            translator = new ObjectTranslator(this, rawL);
            // 下面函数创建了一个这样的table
            // table.__gc = metaFunctions.GcMeta
            // table[0] = 1
            // table[1] = type_id
            // 这个table ref到了LUA_REGISTRYINDEX，type_id是它的返回值，也就是index
            // 此时gettop = 0
            translator.createFunctionMetatable(rawL);
            // 继续向xlua表注入一堆函数
            // 创建了两个空表，ref到LUA_REGISTRYINDEX，index保存到 common_array_meta 和 common_delegate_meta
            translator.OpenLib(rawL);
            // 入pool
            ObjectTranslatorPool.Instance.Add(rawL, translator);
            // luaD_throw(ldo.c)中可以看到panic是错误处理最后的保证，这里是自定义了panic
            LuaAPI.lua_atpanic(rawL, StaticLuaCallbacks.Panic);

#if !XLUA_GENERAL
            // xlua注入了一个print函数，但是很蛋疼，这个函数使用到的地方，都没有直接的导出：print_top
            // 这个注入的方式非常麻烦，print代表的是一个闭包，它的主体函数是csharp_function_wrap，
            // 闭包里面的一个参数是Print这个函数构建的无外部变量闭包
            // 近似可以理解为：_G["print"] = csharp_function_wrap(StaticLuaCallbacks.Print)
            LuaAPI.lua_pushstdcallcfunction(rawL, StaticLuaCallbacks.Print);
            if (0 != LuaAPI.xlua_setglobal(rawL, "print"))
            {
                throw new Exception("call xlua_setglobal fail!");
            }
#endif

            //template engine lib register
            // _G注入一个template表，有compile和execute两个函数
            TemplateEngine.LuaTemplate.OpenLib(rawL);
            // 此时package[searchers]中已经有4个searcher了
            AddSearcher(StaticLuaCallbacks.LoadBuiltinLib, 2); // just after the preload searcher
            AddSearcher(StaticLuaCallbacks.LoadFromCustomLoaders, 3);
#if !XLUA_GENERAL
            AddSearcher(StaticLuaCallbacks.LoadFromResource, 4);
            AddSearcher(StaticLuaCallbacks.LoadFromStreamingAssetsPath, -1);
            // 执行完上面4句后，package[searchers]中的函数结构为，可见1那个是很特别的，估计是lua默认的。而LoadFromStreamingAssetsPath是最不被重视的
            // 1 A
            // 2 LoadBuiltinLib
            // 3 LoadFromCustomLoaders
            // 4 LoadFromResource
            // 5 B
            // 6 C
            // 7 D
            // 8 LoadFromStreamingAssetsPath
#endif
            // 主要作用是定义了CS table,下面代码把CS作为LoadBuiltinLib
            DoString(init_xlua, "Init");
            init_xlua = null;

#if (!UNITY_SWITCH && !UNITY_WEBGL) || UNITY_EDITOR
            // 打开socket，本身没用到，可以屏蔽了
            // AddBuildin的作用是把name&func放到buildin_initer
            // buildin_initer在 LoadBuiltinLib中使用
            // 也就是lua中需要查询文件的时候，第二个调用的函数
            AddBuildin("socket.core", StaticLuaCallbacks.LoadSocketCore);
            AddBuildin("socket", StaticLuaCallbacks.LoadSocketCore);
#endif
            // 创建CS table，给xlua加了一些函数
            // 所有的lua中的CS.XXX.xxx的函数都后走到这段代码中的__index
            AddBuildin("CS", StaticLuaCallbacks.LoadCS);

            // 创建table MT
            LuaAPI.lua_newtable(rawL); //metatable of indexs and newindexs functions
            LuaAPI.xlua_pushasciistring(rawL, "__index");
            LuaAPI.lua_pushstdcallcfunction(rawL, StaticLuaCallbacks.MetaFuncIndex);
            // MT.__index = MetaFuncIndex
            LuaAPI.lua_rawset(rawL, -3);
            // "LuaIndexs"
            LuaAPI.xlua_pushasciistring(rawL, Utils.LuaIndexsFieldName);
            // new table t
            LuaAPI.lua_newtable(rawL);
            // copy MT到-1
            LuaAPI.lua_pushvalue(rawL, -3);
            // setmetatable(t,MT), POP -1的MT
            LuaAPI.lua_setmetatable(rawL, -2);
            // _G[LUA_REGISTRYINDEX][LuaIndexs] = t , pop LuaIndexs & t , MT -1 
            LuaAPI.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);


            // 一样的操作         
            LuaAPI.xlua_pushasciistring(rawL, Utils.LuaNewIndexsFieldName);
            LuaAPI.lua_newtable(rawL);
            LuaAPI.lua_pushvalue(rawL, -3);
            LuaAPI.lua_setmetatable(rawL, -2);
            // _G[LUA_REGISTRYINDEX][LuaNewIndexs] = t , pop LuaIndexs & t , MT -1 
            LuaAPI.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);

            LuaAPI.xlua_pushasciistring(rawL, Utils.LuaClassIndexsFieldName);
            LuaAPI.lua_newtable(rawL);
            LuaAPI.lua_pushvalue(rawL, -3);
            LuaAPI.lua_setmetatable(rawL, -2);
            // _G[LUA_REGISTRYINDEX][LuaClassIndexs] = t , pop LuaIndexs & t , MT -1 
            LuaAPI.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);

            LuaAPI.xlua_pushasciistring(rawL, Utils.LuaClassNewIndexsFieldName);
            LuaAPI.lua_newtable(rawL);
            LuaAPI.lua_pushvalue(rawL, -3);
            LuaAPI.lua_setmetatable(rawL, -2);
            // _G[LUA_REGISTRYINDEX][LuaClassNewIndexs] = t , pop LuaIndexs & t , MT -1 
            LuaAPI.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);
            // POP MT
            LuaAPI.lua_pop(rawL, 1); // pop metatable of indexs and newindexs functions

            LuaAPI.xlua_pushasciistring(rawL, MAIN_SHREAD);
            LuaAPI.lua_pushthread(rawL);
            // _G[LUA_REGISTRYINDEX][xlua_main_thread] = 被改造的top
            LuaAPI.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);

            LuaAPI.xlua_pushasciistring(rawL, CSHARP_NAMESPACE);
            if (0 != LuaAPI.xlua_getglobal(rawL, "CS"))
            {
                throw new Exception("get CS fail!");
            }
            // _G[LUA_REGISTRYINDEX][xlua_csharp_namespace] = init_xlua中创建的CS
            LuaAPI.lua_rawset(rawL, LuaIndexes.LUA_REGISTRYINDEX);

#if !XLUA_GENERAL && (!UNITY_WSA || UNITY_EDITOR)
            // 将System.MonoType存到aliasCfg
            translator.Alias(typeof(Type), "System.MonoType");
#endif
            // _G -1
            if (0 != LuaAPI.xlua_getglobal(rawL, "_G"))
            {
                throw new Exception("get _G fail!");
            }
            // 7 这个数字是registry中的idx，也就是说把G放到了registry，奇怪的操作
            // _G的ud是-1
            // 这里造成的结果有：
            // ObjectTranslator.get_func_with_type填充了数据
            translator.Get(rawL, -1, out _G);
            LuaAPI.lua_pop(rawL, 1);
            // 8
            errorFuncRef = LuaAPI.get_error_func_ref(rawL);

            // 效果是translator.delayWrap中添加gen出来的类的__Register
            if (initers != null)
            {
                for (int i = 0; i < initers.Count; i++)
                {
                    initers[i](this, translator);
                }
            }
            // 给一些全局表提供方法
            translator.CreateArrayMetatable(rawL);
            translator.CreateDelegateMetatable(rawL);
            translator.CreateEnumerablePairs(rawL);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        private static List<Action<LuaEnv, ObjectTranslator>> initers = null;

        /// <summary>
        /// 这里实际是给translator.delayWrap添加__Register，还有添加enum和struct的一些函数
        /// </summary>
        /// <param name="initer"></param>
        public static void AddIniter(Action<LuaEnv, ObjectTranslator> initer)
        {
            if (initers == null)
            {
                initers = new List<Action<LuaEnv, ObjectTranslator>>();
            }
            initers.Add(initer);
        }

        public LuaTable Global
        {
            get
            {
                return _G;
            }
        }

        public T LoadString<T>(byte[] chunk, string chunkName = "chunk", LuaTable env = null)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            if (typeof(T) != typeof(LuaFunction) && !typeof(T).IsSubclassOf(typeof(Delegate)))
            {
                throw new InvalidOperationException(typeof(T).Name + " is not a delegate type nor LuaFunction");
            }
            var _L = L;
            int oldTop = LuaAPI.lua_gettop(_L);

            if (LuaAPI.xluaL_loadbuffer(_L, chunk, chunk.Length, chunkName) != 0)
                ThrowExceptionFromError(oldTop);

            if (env != null)
            {
                env.push(_L);
                LuaAPI.lua_setfenv(_L, -2);
            }

            T result = (T)translator.GetObject(_L, -1, typeof(T));
            LuaAPI.lua_settop(_L, oldTop);

            return result;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public T LoadString<T>(string chunk, string chunkName = "chunk", LuaTable env = null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(chunk);
            return LoadString<T>(bytes, chunkName, env);
        }

        public LuaFunction LoadString(string chunk, string chunkName = "chunk", LuaTable env = null)
        {
            return LoadString<LuaFunction>(chunk, chunkName, env);
        }

        /// <summary>
        /// 执行lua代码
        /// </summary>
        /// <param name="chunk">代码的二进制</param>
        /// <param name="chunkName">luaY_parser中被用于赋值给Proto.source(used for debug information)</param>
        /// <param name="env"></param>
        /// <returns>一个栈上的函数对象</returns>
        public object[] DoString(byte[] chunk, string chunkName = "chunk", LuaTable env = null)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            var _L = L;
            // 记录执行前栈上对象数量
            int oldTop = LuaAPI.lua_gettop(_L);
            // 从registry取出错误处理函数，-1位置
            // errfunc 是当前gettop
            int errFunc = LuaAPI.load_error_func(_L, errorFuncRef);
            // 完成后-1应该是一个lclosure对象
            if (LuaAPI.xluaL_loadbuffer(_L, chunk, chunk.Length, chunkName) == 0)
            {
                if (env != null)
                {
                    // -1位置放一个table，upvalue的信息会被复制到这里，参考lua_setupvalue
                    env.push(_L);
                    // -2是xluaL_loadbuffer中创建的lclosure
                    // 拿到chunk中包含的函数的下标为0的upvalue，放在-1
                    LuaAPI.lua_setfenv(_L, -2);
                }

                // 调用lclosure -2， 参数 -1
                if (LuaAPI.lua_pcall(_L, 0, -1, errFunc) == 0)
                {
                    // 移除errfun
                    LuaAPI.lua_remove(_L, errFunc);
                    // 还原到原来的栈上数量
                    return translator.popValues(_L, oldTop);
                }
                else
                    ThrowExceptionFromError(oldTop);
            }
            else
                ThrowExceptionFromError(oldTop);

            return null;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }
        /// <summary>
        /// 执行lua代码
        /// </summary>
        /// <param name="chunk"></param>
        /// <param name="chunkName">luaY_parser中被用于赋值给Proto.source(used for debug information)</param>
        /// <param name="env"></param>
        /// <returns></returns>
        public object[] DoString(string chunk, string chunkName = "chunk", LuaTable env = null)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(chunk);
            return DoString(bytes, chunkName, env);
        }

        private void AddSearcher(LuaCSFunction searcher, int index)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            var _L = L;
            //insert the loader
            // 结果是栈上保留table package[searchers]
            LuaAPI.xlua_getloaders(_L);
            if (!LuaAPI.lua_istable(_L, -1))
            {
                throw new Exception("Can not set searcher!");
            }
            // 应该是获取searchers表的内容长度
            // AddSearcher(StaticLuaCallbacks.LoadBuiltinLib, 2);
            // 从上面的调用看，index = 2，此时len是4
            uint len = LuaAPI.xlua_objlen(_L, -1);
            index = index < 0 ? (int)(len + index + 2) : index;
            // 以LoadBuiltinLib为例：
            // 这段for把BCD函数移动到345位置，把LoadBuiltinLib放到了2位置
            // AddSearcher(StaticLuaCallbacks.LoadFromCustomLoaders, 3);
            // AddSearcher(StaticLuaCallbacks.LoadFromResource, 4);
            // 上面两句完成后表中函数为：
            // 1 A
            // 2 LoadBuiltinLib
            // 3 LoadFromCustomLoaders
            // 4 LoadFromResource
            // 5 B
            // 6 C
            // 7 D
            // AddSearcher(StaticLuaCallbacks.LoadFromStreamingAssetsPath, -1);
            // index = -1是特殊操作，len = 7 所以 (int)(len + index + 2) = 8
            // 因此这个for条件不满足, e = 7 + 1 = 8 , e == index
            // 所以无需改变函数位置，直接把LoadFromStreamingAssetsPath放到了8
            for (int e = (int)len + 1; e > index; e--)
            {
                LuaAPI.xlua_rawgeti(_L, -1, e - 1);
                LuaAPI.xlua_rawseti(_L, -2, e);
            }
            LuaAPI.lua_pushstdcallcfunction(_L, searcher);
            LuaAPI.xlua_rawseti(_L, -2, index);
            LuaAPI.lua_pop(_L, 1);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        /// <summary>
        /// 放类型到aliasCfg
        /// </summary>
        /// <param name="type"></param>
        /// <param name="alias"></param>
        public void Alias(Type type, string alias)
        {
            translator.Alias(type, alias);
        }

#if !XLUA_GENERAL
        int last_check_point = 0;

        int max_check_per_tick = 20;

        static bool ObjectValidCheck(object obj)
        {
            return (!(obj is UnityEngine.Object)) || ((obj as UnityEngine.Object) != null);
        }

        Func<object, bool> object_valid_checker = new Func<object, bool>(ObjectValidCheck);
#endif

        public void Tick()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            var _L = L;
            lock (refQueue)
            {
                while (refQueue.Count > 0)
                {
                    GCAction gca = refQueue.Dequeue();
                    translator.ReleaseLuaBase(_L, gca.Reference, gca.IsDelegate);
                }
            }
#if !XLUA_GENERAL
            last_check_point = translator.objects.Check(last_check_point, max_check_per_tick, object_valid_checker, translator.reverseMap);
#endif
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        //兼容API
        public void GC()
        {
            Tick();
        }

        public LuaTable NewTable()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            var _L = L;
            int oldTop = LuaAPI.lua_gettop(_L);

            LuaAPI.lua_newtable(_L);
            LuaTable returnVal = (LuaTable)translator.GetObject(_L, -1, typeof(LuaTable));

            LuaAPI.lua_settop(_L, oldTop);
            return returnVal;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        private bool disposed = false;

        public void Dispose()
        {
            FullGc();
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();

            Dispose(true);

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        public virtual void Dispose(bool dispose)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            if (disposed) return;
            Tick();

            if (!translator.AllDelegateBridgeReleased())
            {
                throw new InvalidOperationException("try to dispose a LuaEnv with C# callback!");
            }

            ObjectTranslatorPool.Instance.Remove(L);

            LuaAPI.lua_close(L);
            translator = null;

            rawL = IntPtr.Zero;

            disposed = true;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void ThrowExceptionFromError(int oldTop)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            object err = translator.GetObject(L, -1);
            LuaAPI.lua_settop(L, oldTop);

            // A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
            Exception ex = err as Exception;
            if (ex != null) throw ex;

            // A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
            if (err == null) err = "Unknown Lua Error";
            throw new LuaException(err.ToString());
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        internal struct GCAction
        {
            public int Reference;
            public bool IsDelegate;
        }

        Queue<GCAction> refQueue = new Queue<GCAction>();

        internal void equeueGCAction(GCAction action)
        {
            lock (refQueue)
            {
                refQueue.Enqueue(action);
            }
        }
        // 这段代码定义了两个table：
        // CS：所有Csharp定义的类的注入都在这个里面
        // base：好像没咋用
        // 给xlua扩展了一些函数
        private string init_xlua = @" 
            local metatable = {}
            local rawget = rawget
            local setmetatable = setmetatable
            local import_type = xlua.import_type
            local import_generic_type = xlua.import_generic_type
            local load_assembly = xlua.load_assembly
            -- 开始定义metatable
            -- 以CS.UnityEngine.GameObject为例
            function metatable:__index(key) 
                -- 第一次加载时fqn是nil
                local fqn = rawget(self,'.fqn')

                -- CS.UnityEngine时fqn = UnityEngine
                -- CS.UnityEngine.GameObject时fqn = UnityEngine.GameObject
                fqn = ((fqn and fqn .. '.') or '') .. key
               
                -- -1 true
                -- -2 'UnityEngine.GameObject'
                local obj = import_type(fqn)
                -- true作为返回值被pop
                

                --  CS.UnityEngine时找不到的，是个assembly
                if obj == nil then
                    -- It might be an assembly, so we load it too.
                    -- obj = { ['.fqn'] = UnityEngine } 
                    obj = { ['.fqn'] = fqn, ['tsai'] = fqn} 
                    setmetatable(obj, metatable)
                elseif obj == true then
                    -- CS.UnityEngine.GameObject时会创建成功，
                    -- 此时是返回CS['UnityEngine']['GameObject']
                    -- self就是CS['UnityEngine']
                    print('KEY = '..key)
                    return rawget(self, key)
                end

                -- Cache this lookup
                -- CS.UnityEngine时 CS[UnityEngine] = obj
                rawset(self, key, obj)
                return obj
            end

            function metatable:__newindex()
                error('No such type: ' .. rawget(self,'.fqn'), 2)
            end

            -- A non-type has been called; e.g. foo = System.Foo()
            function metatable:__call(...)
                local n = select('#', ...)
                local fqn = rawget(self,'.fqn')
                if n > 0 then
                    local gt = import_generic_type(fqn, ...)
                    if gt then
                        return rawget(CS, gt)
                    end
                end
                error('No such type: ' .. fqn, 2)
            end

            CS = CS or {}
            -- test
            CS.tsai = 'CS'
            -- end
            setmetatable(CS, metatable)

            typeof = function(t) return t.UnderlyingSystemType end
            cast = xlua.cast
            if not setfenv or not getfenv then
                local function getfunction(level)
                    local info = debug.getinfo(level + 1, 'f')
                    return info and info.func
                end

                function setfenv(fn, env)
                  if type(fn) == 'number' then fn = getfunction(fn + 1) end
                  local i = 1
                  while true do
                    local name = debug.getupvalue(fn, i)
                    if name == '_ENV' then
                      debug.upvaluejoin(fn, i, (function()
                        return env
                      end), 1)
                      break
                    elseif not name then
                      break
                    end

                    i = i + 1
                  end

                  return fn
                end

                function getfenv(fn)
                  if type(fn) == 'number' then fn = getfunction(fn + 1) end
                  local i = 1
                  while true do
                    local name, val = debug.getupvalue(fn, i)
                    if name == '_ENV' then
                      return val
                    elseif not name then
                      break
                    end
                    i = i + 1
                  end
                end
            end

            xlua.hotfix = function(cs, field, func)
                if func == nil then func = false end
                local tbl = (type(field) == 'table') and field or {[field] = func}
                for k, v in pairs(tbl) do
                    local cflag = ''
                    if k == '.ctor' then
                        cflag = '_c'
                        k = 'ctor'
                    end
                    local f = type(v) == 'function' and v or nil
                    xlua.access(cs, cflag .. '__Hotfix0_'..k, f) -- at least one
                    pcall(function()
                        for i = 1, 99 do
                            xlua.access(cs, cflag .. '__Hotfix'..i..'_'..k, f)
                        end
                    end)
                end
                xlua.private_accessible(cs)
            end
            xlua.getmetatable = function(cs)
                return xlua.metatable_operation(cs)
            end
            xlua.setmetatable = function(cs, mt)
                return xlua.metatable_operation(cs, mt)
            end
            xlua.setclass = function(parent, name, impl)
                impl.UnderlyingSystemType = parent[name].UnderlyingSystemType
                rawset(parent, name, impl)
            end
            
            local base_mt = {
                __index = function(t, k)
                    local csobj = t['__csobj']
                    local func = csobj['<>xLuaBaseProxy_'..k]
                    return function(_, ...)
                         return func(csobj, ...)
                    end
                end
            }
            base = function(csobj)
                return setmetatable({__csobj = csobj}, base_mt)
            end
            ";

        public delegate byte[] CustomLoader(ref string filepath);

        internal List<CustomLoader> customLoaders = new List<CustomLoader>();

        //loader : CustomLoader， filepath参数：（ref类型）输入是require的参数，如果需要支持调试，需要输出真实路径。
        //                        返回值：如果返回null，代表加载该源下无合适的文件，否则返回UTF8编码的byte[]
        public void AddLoader(CustomLoader loader)
        {
            customLoaders.Add(loader);
        }

        internal Dictionary<string, LuaCSFunction> buildin_initer = new Dictionary<string, LuaCSFunction>();

        public void AddBuildin(string name, LuaCSFunction initer)
        {
            if (!Utils.IsStaticPInvokeCSFunction(initer))
            {
                throw new Exception("initer must be static and has MonoPInvokeCallback Attribute!");
            }
            buildin_initer.Add(name, initer);
        }

        //The garbage-collector pause controls how long the collector waits before starting a new cycle. 
        //Larger values make the collector less aggressive. Values smaller than 100 mean the collector 
        //will not wait to start a new cycle. A value of 200 means that the collector waits for the total 
        //memory in use to double before starting a new cycle.
        public int GcPause
        {
            get
            {
#if THREAD_SAFE || HOTFIX_ENABLE
                lock (luaEnvLock)
                {
#endif
                int val = LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSETPAUSE, 200);
                LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSETPAUSE, val);
                return val;
#if THREAD_SAFE || HOTFIX_ENABLE
                }
#endif
            }
            set
            {
#if THREAD_SAFE || HOTFIX_ENABLE
                lock (luaEnvLock)
                {
#endif
                LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSETPAUSE, value);
#if THREAD_SAFE || HOTFIX_ENABLE
                }
#endif
            }
        }

        //The step multiplier controls the relative speed of the collector relative to memory allocation. 
        //Larger values make the collector more aggressive but also increase the size of each incremental 
        //step. Values smaller than 100 make the collector too slow and can result in the collector never 
        //finishing a cycle. The default, 200, means that the collector runs at "twice" the speed of memory 
        //allocation.
        public int GcStepmul
        {
            get
            {
#if THREAD_SAFE || HOTFIX_ENABLE
                lock (luaEnvLock)
                {
#endif
                int val = LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSETSTEPMUL, 200);
                LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSETSTEPMUL, val);
                return val;
#if THREAD_SAFE || HOTFIX_ENABLE
                }
#endif
            }
            set
            {
#if THREAD_SAFE || HOTFIX_ENABLE
                lock (luaEnvLock)
                {
#endif
                LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSETSTEPMUL, value);
#if THREAD_SAFE || HOTFIX_ENABLE
                }
#endif
            }
        }

        public void FullGc()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCCOLLECT, 0);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void StopGc()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSTOP, 0);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void RestartGc()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCRESTART, 0);
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool GcStep(int data)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnvLock)
            {
#endif
            return LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCSTEP, data) != 0;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int Memroy
        {
            get
            {
#if THREAD_SAFE || HOTFIX_ENABLE
                lock (luaEnvLock)
                {
#endif
                return LuaAPI.lua_gc(L, LuaGCOptions.LUA_GCCOUNT, 0);
#if THREAD_SAFE || HOTFIX_ENABLE
                }
#endif
            }
        }
    }
}