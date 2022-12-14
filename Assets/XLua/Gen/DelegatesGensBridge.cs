#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using System;


namespace XLua
{
    public partial class DelegateBridge : DelegateBridgeBase
    {
        /// <summary>
        /// Action真正的代表的函数，也就是说执行Action的时候执行的是这个函数。
        /// </summary>
        public void __Gen_Delegate_Imp0()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;

            //LUA_API int pcall_prepare(lua_State* L, int error_func_ref, int func_ref)
            //{
            //    lua_rawgeti(L, LUA_REGISTRYINDEX, error_func_ref);
            //    lua_rawgeti(L, LUA_REGISTRYINDEX, func_ref);
            //    // lua_gettop(L) = 2, 1是栈底，也就是error_func_ref得到的数据
            //    return lua_gettop(L) - 1;
            //}
            // 这里看出来就是为了拿到errFunc的位置
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            // 参数依次为L，参数个数，返回值个数，错误处理函数处于栈的位置
            PCall(L, 0, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public double __Gen_Delegate_Imp1(double p0, double p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushnumber(L, p0);
            LuaAPI.lua_pushnumber(L, p1);

            PCall(L, 2, 1, errFunc);


            double __gen_ret = LuaAPI.lua_tonumber(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp2(string p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushstring(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp3(double p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushnumber(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp4(int p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.xlua_pushinteger(L, p0);

            PCall(L, 1, 1, errFunc);


            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public UnityEngine.Vector3 __Gen_Delegate_Imp5(UnityEngine.Vector3 p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushUnityEngineVector3(L, p0);

            PCall(L, 1, 1, errFunc);


            UnityEngine.Vector3 __gen_ret; translator.Get(L, errFunc + 1, out __gen_ret);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public XLuaTest.MyStruct __Gen_Delegate_Imp6(XLuaTest.MyStruct p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushXLuaTestMyStruct(L, p0);

            PCall(L, 1, 1, errFunc);


            XLuaTest.MyStruct __gen_ret; translator.Get(L, errFunc + 1, out __gen_ret);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public XLuaTest.MyEnum __Gen_Delegate_Imp7(XLuaTest.MyEnum p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushXLuaTestMyEnum(L, p0);

            PCall(L, 1, 1, errFunc);


            XLuaTest.MyEnum __gen_ret; translator.Get(L, errFunc + 1, out __gen_ret);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public decimal __Gen_Delegate_Imp8(decimal p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushDecimal(L, p0);

            PCall(L, 1, 1, errFunc);


            decimal __gen_ret; translator.Get(L, errFunc + 1, out __gen_ret);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp9(System.Array p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp10(bool p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushboolean(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public XLuaTest.InvokeLua.ICalc __Gen_Delegate_Imp11(int p0, string[] p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            LuaAPI.xlua_pushinteger(L, p0);
            if (p1 != null) { for (int __gen_i = 0; __gen_i < p1.Length; ++__gen_i) LuaAPI.lua_pushstring(L, p1[__gen_i]); };

            PCall(L, 1 + (p1 == null ? 0 : p1.Length), 1, errFunc);


            XLuaTest.InvokeLua.ICalc __gen_ret = (XLuaTest.InvokeLua.ICalc)translator.GetObject(L, errFunc + 1, typeof(XLuaTest.InvokeLua.ICalc));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp12(UnityEngine.Camera p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp13(float[] p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp14(int p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.xlua_pushinteger(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp15(string p0, bool p1, string p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushstring(L, p0);
            LuaAPI.lua_pushboolean(L, p1);
            LuaAPI.lua_pushstring(L, p2);

            PCall(L, 3, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp16(string p0, string p1, UnityEngine.LogType p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            LuaAPI.lua_pushstring(L, p0);
            LuaAPI.lua_pushstring(L, p1);
            translator.Push(L, p2);

            PCall(L, 3, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool __Gen_Delegate_Imp17()
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);


            PCall(L, 0, 1, errFunc);


            bool __gen_ret = LuaAPI.lua_toboolean(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp18(UnityEngine.CullingGroupEvent p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp19(UnityEngine.ReflectionProbe p0, UnityEngine.ReflectionProbe.ReflectionProbeEvent p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);
            translator.Push(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp20(UnityEngine.Cubemap p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp21(UnityEngine.AsyncOperation p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp22(UnityEngine.RectTransform p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp23(UnityEngine.Font p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp24(bool p0, bool p1, int p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushboolean(L, p0);
            LuaAPI.lua_pushboolean(L, p1);
            LuaAPI.xlua_pushinteger(L, p2);

            PCall(L, 3, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public char __Gen_Delegate_Imp25(string p0, int p1, char p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushstring(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            LuaAPI.xlua_pushinteger(L, p2);

            PCall(L, 3, 1, errFunc);


            char __gen_ret = (char)LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public float __Gen_Delegate_Imp26(UnityEngine.UI.ILayoutElement p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 1, errFunc);


            float __gen_ret = (float)LuaAPI.lua_tonumber(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp27(System.IntPtr p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);

            PCall(L, 1, 1, errFunc);


            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool __Gen_Delegate_Imp28(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            bool __gen_ret = LuaAPI.lua_toboolean(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public object __Gen_Delegate_Imp29(System.IntPtr p0, int p1, object p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            translator.PushAny(L, p2);

            PCall(L, 3, 1, errFunc);


            object __gen_ret = translator.GetObject(L, errFunc + 1, typeof(object));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public byte[] __Gen_Delegate_Imp30(ref string p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushstring(L, p0);

            PCall(L, 1, 2, errFunc);

            p0 = LuaAPI.lua_tostring(L, errFunc + 2);

            byte[] __gen_ret = LuaAPI.lua_tobytes(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp31(XLua.LuaEnv p0, XLua.ObjectTranslator p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);
            translator.Push(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp32(System.IAsyncResult p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool __Gen_Delegate_Imp33(object p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 1, errFunc);


            bool __gen_ret = LuaAPI.lua_toboolean(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp34(System.IntPtr p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public XLua.LuaBase __Gen_Delegate_Imp35(int p0, XLua.LuaEnv p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            LuaAPI.xlua_pushinteger(L, p0);
            translator.Push(L, p1);

            PCall(L, 2, 1, errFunc);


            XLua.LuaBase __gen_ret = (XLua.LuaBase)translator.GetObject(L, errFunc + 1, typeof(XLua.LuaBase));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp36(int p0, double p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.xlua_pushinteger(L, p0);
            LuaAPI.lua_pushnumber(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp37(object p0, XLuaTest.PropertyChangedEventArgs p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            translator.Push(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool __Gen_Delegate_Imp38(System.Reflection.MethodInfo p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            bool __gen_ret = LuaAPI.lua_toboolean(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp39(System.Reflection.MethodInfo p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public System.Delegate __Gen_Delegate_Imp40(XLua.DelegateBridgeBase p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            System.Delegate __gen_ret = translator.GetDelegate<System.Delegate>(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public System.Type __Gen_Delegate_Imp41(System.Reflection.ParameterInfo p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            System.Type __gen_ret = (System.Type)translator.GetObject(L, errFunc + 1, typeof(System.Type));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp42(System.IntPtr p0, byte p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp43(System.IntPtr p0, sbyte p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp44(System.IntPtr p0, char p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp45(System.IntPtr p0, short p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp46(System.IntPtr p0, ushort p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp47(System.IntPtr p0, float p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.lua_pushnumber(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public byte __Gen_Delegate_Imp48(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            byte __gen_ret = (byte)LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public sbyte __Gen_Delegate_Imp49(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            sbyte __gen_ret = (sbyte)LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public char __Gen_Delegate_Imp50(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            char __gen_ret = (char)LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public short __Gen_Delegate_Imp51(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            short __gen_ret = (short)LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public ushort __Gen_Delegate_Imp52(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            ushort __gen_ret = (ushort)LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public float __Gen_Delegate_Imp53(System.IntPtr p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.lua_pushlightuserdata(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 1, errFunc);


            float __gen_ret = (float)LuaAPI.lua_tonumber(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public string __Gen_Delegate_Imp54(System.Type p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            string __gen_ret = LuaAPI.lua_tostring(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool __Gen_Delegate_Imp55(System.Type p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            bool __gen_ret = LuaAPI.lua_toboolean(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public bool __Gen_Delegate_Imp56(System.Reflection.PropertyInfo p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            bool __gen_ret = LuaAPI.lua_toboolean(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> __Gen_Delegate_Imp57(System.Type p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> __gen_ret = (System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>)translator.GetObject(L, errFunc + 1, typeof(System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public System.Type __Gen_Delegate_Imp62(System.Linq.IGrouping<System.Type, System.Reflection.MethodInfo> p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 1, errFunc);


            System.Type __gen_ret = (System.Type)translator.GetObject(L, errFunc + 1, typeof(System.Type));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> __Gen_Delegate_Imp63(System.Linq.IGrouping<System.Type, System.Reflection.MethodInfo> p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 1, errFunc);


            System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo> __gen_ret = (System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>)translator.GetObject(L, errFunc + 1, typeof(System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public string __Gen_Delegate_Imp64(System.Reflection.MethodInfo p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            string __gen_ret = LuaAPI.lua_tostring(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public System.Reflection.MethodInfo __Gen_Delegate_Imp65(System.Reflection.MethodInfo p0, System.Reflection.MethodInfo p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);
            translator.Push(L, p1);

            PCall(L, 2, 1, errFunc);


            System.Reflection.MethodInfo __gen_ret = (System.Reflection.MethodInfo)translator.GetObject(L, errFunc + 1, typeof(System.Reflection.MethodInfo));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp69(object p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp70(object p0, int p1, int p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            LuaAPI.xlua_pushinteger(L, p2);

            PCall(L, 3, 1, errFunc);


            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public UnityEngine.Vector3 __Gen_Delegate_Imp71(object p0, UnityEngine.Vector3 p1, UnityEngine.Vector3 p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            translator.PushUnityEngineVector3(L, p1);
            translator.PushUnityEngineVector3(L, p2);

            PCall(L, 3, 1, errFunc);


            UnityEngine.Vector3 __gen_ret; translator.Get(L, errFunc + 1, out __gen_ret);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp72(object p0, int p1, out double p2, ref string p3)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            LuaAPI.lua_pushstring(L, p3);

            PCall(L, 3, 3, errFunc);

            p2 = LuaAPI.lua_tonumber(L, errFunc + 2);
            p3 = LuaAPI.lua_tostring(L, errFunc + 3);

            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp73(object p0, int p1, out double p2, ref string p3, object p4)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            LuaAPI.lua_pushstring(L, p3);
            translator.PushAny(L, p4);

            PCall(L, 4, 3, errFunc);

            p2 = LuaAPI.lua_tonumber(L, errFunc + 2);
            p3 = LuaAPI.lua_tostring(L, errFunc + 3);

            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp74(object p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public string __Gen_Delegate_Imp75(object p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 1, errFunc);


            string __gen_ret = LuaAPI.lua_tostring(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public UnityEngine.GameObject __Gen_Delegate_Imp76(XLuaTest.StructTest p0, int p1, object p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            translator.PushAny(L, p2);

            PCall(L, 3, 1, errFunc);


            UnityEngine.GameObject __gen_ret = (UnityEngine.GameObject)translator.GetObject(L, errFunc + 1, typeof(UnityEngine.GameObject));
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public string __Gen_Delegate_Imp77(XLuaTest.StructTest p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);

            PCall(L, 1, 1, errFunc);


            string __gen_ret = LuaAPI.lua_tostring(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp78(XLuaTest.StructTest p0, object p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.Push(L, p0);
            translator.PushAny(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp79(object p0)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);

            PCall(L, 1, 1, errFunc);


            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp80(object p0, object p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            translator.PushAny(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public int __Gen_Delegate_Imp81(object p0, object p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            translator.PushAny(L, p1);

            PCall(L, 2, 1, errFunc);


            int __gen_ret = LuaAPI.xlua_tointeger(L, errFunc + 1);
            LuaAPI.lua_settop(L, errFunc - 1);
            return __gen_ret;
#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp82(object p0, object p1, int p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            translator.PushAny(L, p1);
            LuaAPI.xlua_pushinteger(L, p2);

            PCall(L, 3, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp83(int p0, int p1)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);

            LuaAPI.xlua_pushinteger(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);

            PCall(L, 2, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }

        public void __Gen_Delegate_Imp84(object p0, int p1, int p2)
        {
#if THREAD_SAFE || HOTFIX_ENABLE
            lock (luaEnv.luaEnvLock)
            {
#endif
            RealStatePtr L = luaEnv.rawL;
            int errFunc = LuaAPI.pcall_prepare(L, errorFuncRef, luaReference);
            ObjectTranslator translator = luaEnv.translator;
            translator.PushAny(L, p0);
            LuaAPI.xlua_pushinteger(L, p1);
            LuaAPI.xlua_pushinteger(L, p2);

            PCall(L, 3, 0, errFunc);



            LuaAPI.lua_settop(L, errFunc - 1);

#if THREAD_SAFE || HOTFIX_ENABLE
            }
#endif
        }


        static DelegateBridge()
        {
            Gen_Flag = true;
        }

        public override Delegate GetDelegateByType(Type type)
        {

            if (type == typeof(System.Action))
            {
                // 总结一下，这里返回的Action对象中有一个Method属性，其值为__Gen_Delegate_Imp0
                // fullname = XLua.DelegateBridge.__Gen_Delegate_Imp0()
                // 当调用Action时，可以是Invoke时，真正执行的是__Gen_Delegate_Imp0
                // 而在__Gen_Delegate_Imp0中，本质是执行luaReference位置的函数。
                // 怎么个感觉呢，Action中仿佛有个闭包__Gen_Delegate_Imp0，里面包含了errorFuncRef, luaReference
                // 然后执行的时候PCall就好了。其实纵观__Gen_Delegate_ImpN这一系列的函数，就是这么个逻辑。
                return new System.Action(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.Events.UnityAction))
            {
                return new UnityEngine.Events.UnityAction(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.Application.LowMemoryCallback))
            {
                return new UnityEngine.Application.LowMemoryCallback(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.Display.DisplaysUpdatedDelegate))
            {
                return new UnityEngine.Display.DisplaysUpdatedDelegate(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.Font.FontTextureRebuildCallback))
            {
                return new UnityEngine.Font.FontTextureRebuildCallback(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.CanvasRenderer.OnRequestRebuild))
            {
                return new UnityEngine.CanvasRenderer.OnRequestRebuild(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.Canvas.WillRenderCanvases))
            {
                return new UnityEngine.Canvas.WillRenderCanvases(__Gen_Delegate_Imp0);
            }

            if (type == typeof(UnityEngine.RemoteSettings.UpdatedEventHandler))
            {
                return new UnityEngine.RemoteSettings.UpdatedEventHandler(__Gen_Delegate_Imp0);
            }

            if (type == typeof(System.Func<double, double, double>))
            {
                return new System.Func<double, double, double>(__Gen_Delegate_Imp1);
            }

            if (type == typeof(System.Action<string>))
            {
                return new System.Action<string>(__Gen_Delegate_Imp2);
            }

            if (type == typeof(System.Action<double>))
            {
                return new System.Action<double>(__Gen_Delegate_Imp3);
            }

            if (type == typeof(XLuaTest.IntParam))
            {
                return new XLuaTest.IntParam(__Gen_Delegate_Imp4);
            }

            if (type == typeof(XLuaTest.Vector3Param))
            {
                return new XLuaTest.Vector3Param(__Gen_Delegate_Imp5);
            }

            if (type == typeof(XLuaTest.CustomValueTypeParam))
            {
                return new XLuaTest.CustomValueTypeParam(__Gen_Delegate_Imp6);
            }

            if (type == typeof(XLuaTest.EnumParam))
            {
                return new XLuaTest.EnumParam(__Gen_Delegate_Imp7);
            }

            if (type == typeof(XLuaTest.DecimalParam))
            {
                return new XLuaTest.DecimalParam(__Gen_Delegate_Imp8);
            }

            if (type == typeof(XLuaTest.ArrayAccess))
            {
                return new XLuaTest.ArrayAccess(__Gen_Delegate_Imp9);
            }

            if (type == typeof(System.Action<bool>))
            {
                return new System.Action<bool>(__Gen_Delegate_Imp10);
            }

            if (type == typeof(UnityEngine.AudioSettings.AudioConfigurationChangeHandler))
            {
                return new UnityEngine.AudioSettings.AudioConfigurationChangeHandler(__Gen_Delegate_Imp10);
            }

            if (type == typeof(XLuaTest.InvokeLua.CalcNew))
            {
                return new XLuaTest.InvokeLua.CalcNew(__Gen_Delegate_Imp11);
            }

            if (type == typeof(UnityEngine.Camera.CameraCallback))
            {
                return new UnityEngine.Camera.CameraCallback(__Gen_Delegate_Imp12);
            }

            if (type == typeof(UnityEngine.AudioClip.PCMReaderCallback))
            {
                return new UnityEngine.AudioClip.PCMReaderCallback(__Gen_Delegate_Imp13);
            }

            if (type == typeof(UnityEngine.AudioClip.PCMSetPositionCallback))
            {
                return new UnityEngine.AudioClip.PCMSetPositionCallback(__Gen_Delegate_Imp14);
            }

            if (type == typeof(UnityEngine.Application.AdvertisingIdentifierCallback))
            {
                return new UnityEngine.Application.AdvertisingIdentifierCallback(__Gen_Delegate_Imp15);
            }

            if (type == typeof(UnityEngine.Application.LogCallback))
            {
                return new UnityEngine.Application.LogCallback(__Gen_Delegate_Imp16);
            }

            if (type == typeof(System.Func<bool>))
            {
                return new System.Func<bool>(__Gen_Delegate_Imp17);
            }

            if (type == typeof(UnityEngine.CullingGroup.StateChanged))
            {
                return new UnityEngine.CullingGroup.StateChanged(__Gen_Delegate_Imp18);
            }

            if (type == typeof(System.Action<UnityEngine.ReflectionProbe, UnityEngine.ReflectionProbe.ReflectionProbeEvent>))
            {
                return new System.Action<UnityEngine.ReflectionProbe, UnityEngine.ReflectionProbe.ReflectionProbeEvent>(__Gen_Delegate_Imp19);
            }

            if (type == typeof(System.Action<UnityEngine.Cubemap>))
            {
                return new System.Action<UnityEngine.Cubemap>(__Gen_Delegate_Imp20);
            }

            if (type == typeof(System.Action<UnityEngine.AsyncOperation>))
            {
                return new System.Action<UnityEngine.AsyncOperation>(__Gen_Delegate_Imp21);
            }

            if (type == typeof(UnityEngine.RectTransform.ReapplyDrivenProperties))
            {
                return new UnityEngine.RectTransform.ReapplyDrivenProperties(__Gen_Delegate_Imp22);
            }

            if (type == typeof(System.Action<UnityEngine.Font>))
            {
                return new System.Action<UnityEngine.Font>(__Gen_Delegate_Imp23);
            }

            if (type == typeof(System.Action<bool, bool, int>))
            {
                return new System.Action<bool, bool, int>(__Gen_Delegate_Imp24);
            }

            if (type == typeof(UnityEngine.UI.InputField.OnValidateInput))
            {
                return new UnityEngine.UI.InputField.OnValidateInput(__Gen_Delegate_Imp25);
            }

            if (type == typeof(System.Func<UnityEngine.UI.ILayoutElement, float>))
            {
                return new System.Func<UnityEngine.UI.ILayoutElement, float>(__Gen_Delegate_Imp26);
            }

            if (type == typeof(XLua.LuaDLL.lua_CSFunction))
            {
                return new XLua.LuaDLL.lua_CSFunction(__Gen_Delegate_Imp27);
            }

            if (type == typeof(XLua.ObjectCheck))
            {
                return new XLua.ObjectCheck(__Gen_Delegate_Imp28);
            }

            if (type == typeof(XLua.ObjectCast))
            {
                return new XLua.ObjectCast(__Gen_Delegate_Imp29);
            }

            if (type == typeof(XLua.LuaEnv.CustomLoader))
            {
                return new XLua.LuaEnv.CustomLoader(__Gen_Delegate_Imp30);
            }

            if (type == typeof(System.Action<XLua.LuaEnv, XLua.ObjectTranslator>))
            {
                return new System.Action<XLua.LuaEnv, XLua.ObjectTranslator>(__Gen_Delegate_Imp31);
            }

            if (type == typeof(System.AsyncCallback))
            {
                return new System.AsyncCallback(__Gen_Delegate_Imp32);
            }

            if (type == typeof(System.Func<object, bool>))
            {
                return new System.Func<object, bool>(__Gen_Delegate_Imp33);
            }

            if (type == typeof(System.Action<System.IntPtr>))
            {
                return new System.Action<System.IntPtr>(__Gen_Delegate_Imp34);
            }

            if (type == typeof(System.Func<int, XLua.LuaEnv, XLua.LuaBase>))
            {
                return new System.Func<int, XLua.LuaEnv, XLua.LuaBase>(__Gen_Delegate_Imp35);
            }

            if (type == typeof(System.Action<int, double>))
            {
                return new System.Action<int, double>(__Gen_Delegate_Imp36);
            }

            if (type == typeof(System.EventHandler<XLuaTest.PropertyChangedEventArgs>))
            {
                return new System.EventHandler<XLuaTest.PropertyChangedEventArgs>(__Gen_Delegate_Imp37);
            }

            if (type == typeof(System.Func<System.Reflection.MethodInfo, bool>))
            {
                return new System.Func<System.Reflection.MethodInfo, bool>(__Gen_Delegate_Imp38);
            }

            if (type == typeof(System.Func<System.Reflection.MethodInfo, int>))
            {
                return new System.Func<System.Reflection.MethodInfo, int>(__Gen_Delegate_Imp39);
            }

            if (type == typeof(System.Func<XLua.DelegateBridgeBase, System.Delegate>))
            {
                return new System.Func<XLua.DelegateBridgeBase, System.Delegate>(__Gen_Delegate_Imp40);
            }

            if (type == typeof(System.Func<System.Reflection.ParameterInfo, System.Type>))
            {
                return new System.Func<System.Reflection.ParameterInfo, System.Type>(__Gen_Delegate_Imp41);
            }

            if (type == typeof(System.Action<System.IntPtr, byte>))
            {
                return new System.Action<System.IntPtr, byte>(__Gen_Delegate_Imp42);
            }

            if (type == typeof(System.Action<System.IntPtr, sbyte>))
            {
                return new System.Action<System.IntPtr, sbyte>(__Gen_Delegate_Imp43);
            }

            if (type == typeof(System.Action<System.IntPtr, char>))
            {
                return new System.Action<System.IntPtr, char>(__Gen_Delegate_Imp44);
            }

            if (type == typeof(System.Action<System.IntPtr, short>))
            {
                return new System.Action<System.IntPtr, short>(__Gen_Delegate_Imp45);
            }

            if (type == typeof(System.Action<System.IntPtr, ushort>))
            {
                return new System.Action<System.IntPtr, ushort>(__Gen_Delegate_Imp46);
            }

            if (type == typeof(System.Action<System.IntPtr, float>))
            {
                return new System.Action<System.IntPtr, float>(__Gen_Delegate_Imp47);
            }

            if (type == typeof(System.Func<System.IntPtr, int, byte>))
            {
                return new System.Func<System.IntPtr, int, byte>(__Gen_Delegate_Imp48);
            }

            if (type == typeof(System.Func<System.IntPtr, int, sbyte>))
            {
                return new System.Func<System.IntPtr, int, sbyte>(__Gen_Delegate_Imp49);
            }

            if (type == typeof(System.Func<System.IntPtr, int, char>))
            {
                return new System.Func<System.IntPtr, int, char>(__Gen_Delegate_Imp50);
            }

            if (type == typeof(System.Func<System.IntPtr, int, short>))
            {
                return new System.Func<System.IntPtr, int, short>(__Gen_Delegate_Imp51);
            }

            if (type == typeof(System.Func<System.IntPtr, int, ushort>))
            {
                return new System.Func<System.IntPtr, int, ushort>(__Gen_Delegate_Imp52);
            }

            if (type == typeof(System.Func<System.IntPtr, int, float>))
            {
                return new System.Func<System.IntPtr, int, float>(__Gen_Delegate_Imp53);
            }

            if (type == typeof(System.Func<System.Type, string>))
            {
                return new System.Func<System.Type, string>(__Gen_Delegate_Imp54);
            }

            if (type == typeof(System.Func<System.Type, bool>))
            {
                return new System.Func<System.Type, bool>(__Gen_Delegate_Imp55);
            }

            if (type == typeof(System.Func<System.Reflection.PropertyInfo, bool>))
            {
                return new System.Func<System.Reflection.PropertyInfo, bool>(__Gen_Delegate_Imp56);
            }

            if (type == typeof(System.Func<System.Type, System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>>))
            {
                return new System.Func<System.Type, System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>>(__Gen_Delegate_Imp57);
            }

            if (type == typeof(System.Func<System.Linq.IGrouping<System.Type, System.Reflection.MethodInfo>, System.Type>))
            {
                return new System.Func<System.Linq.IGrouping<System.Type, System.Reflection.MethodInfo>, System.Type>(__Gen_Delegate_Imp62);
            }

            if (type == typeof(System.Func<System.Linq.IGrouping<System.Type, System.Reflection.MethodInfo>, System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>>))
            {
                return new System.Func<System.Linq.IGrouping<System.Type, System.Reflection.MethodInfo>, System.Collections.Generic.IEnumerable<System.Reflection.MethodInfo>>(__Gen_Delegate_Imp63);
            }

            if (type == typeof(System.Func<System.Reflection.MethodInfo, string>))
            {
                return new System.Func<System.Reflection.MethodInfo, string>(__Gen_Delegate_Imp64);
            }

            if (type == typeof(System.Func<System.Reflection.MethodInfo, System.Reflection.MethodInfo, System.Reflection.MethodInfo>))
            {
                return new System.Func<System.Reflection.MethodInfo, System.Reflection.MethodInfo, System.Reflection.MethodInfo>(__Gen_Delegate_Imp65);
            }

            return null;
        }
    }

}