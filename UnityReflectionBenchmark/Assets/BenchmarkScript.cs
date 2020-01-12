using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

public class BenchmarkScript : MonoBehaviour
{
    struct TestStruct
    {
        public int a;
        public int b;
        public int c;
        public int d;
        public int e;
        public int f;
    }

    class TestClass
    {
        public int a;
        public int b;
        public int c;
        public int d;
        public int e;
        public int f;
    }
    public delegate object ObjectActivator();
    private readonly Dictionary<string, ObjectActivator> expressionCreateInstanceFuncs = new Dictionary<string, ObjectActivator>();
    private readonly Dictionary<string, Func<object>> expressionCreateInstanceFuncs2 = new Dictionary<string, Func<object>>();
    private readonly Dictionary<string, DynamicMethod> ilCreateInstanceFuncs = new Dictionary<string, DynamicMethod>();
    public delegate void TestDelegate<T1, T2>(T1 a, T2 b);

    public int benchmarkLoopCount = 100000;
    
    private void Update()
    {
        if (Input.anyKeyDown)
        {
            RunBenchmark();
            UnityEngine.Debug.Log("--- Done ---");
        }
    }

    private void RunBenchmark()
    {
        BenchmarkIL();
        BenchmarkExpression();
        BenchmarkExpression2();
        BenchmarkActivatorCreateInstance();
        BenchmarkMethodInfoInvoke();
        BenchmarkDelegateDynamicInvoke();
        BenchmarkMethodInvoke();
        BenchmarkMethodInvoke2();
        BenchmarkDelegateInvoke();
        BenchmarkDelegateInvoke2();
    }

    private void TestFunction(int a, int b)
    {
        int c = a + b;
    }

    private void BenchmarkMethodInfoInvoke()
    {
        MethodInfo methodInfo = GetType().GetMethod("TestFunction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        StopWatch("BenchmarkMethodInfoInvoke", () => {
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                methodInfo.Invoke(this, new object[] { 1, 1 });
            }
        });
    }

    private void BenchmarkDelegateDynamicInvoke()
    {
        MethodInfo methodInfo = GetType().GetMethod("TestFunction", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        Type[] types = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
        Delegate del = Delegate.CreateDelegate(Expression.GetActionType(types), this, "TestFunction");
        StopWatch("BenchmarkDelegateDynamicInvoke", () => {
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                del.DynamicInvoke(new object[] { 1, 1 });
            }
        });
    }

    private void BenchmarkMethodInvoke()
    {
        object var1 = 1;
        object var2 = 1;
        StopWatch("BenchmarkMethodInvoke", () => {
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                TestFunction((int)var1, (int)var2);
            }
        });
    }

    private void BenchmarkMethodInvoke2()
    {
        int var1 = 1;
        int var2 = 1;
        StopWatch("BenchmarkMethodInvoke2", () => {
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                TestFunction(var1, var2);
            }
        });
    }

    private void BenchmarkDelegateInvoke()
    {
        TestDelegate<int, int> test = TestFunction;
        object var1 = 1;
        object var2 = 1;
        StopWatch("BenchmarkDelegateInvoke", () => {
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                test.Invoke((int)var1, (int)var2);
            }
        });
    }

    private void BenchmarkDelegateInvoke2()
    {
        TestDelegate<int, int> test = TestFunction;
        int var1 = 1;
        int var2 = 1;
        StopWatch("BenchmarkDelegateInvoke2", () => {
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                test.Invoke(var1, var2);
            }
        });
    }

    private void BenchmarkActivatorCreateInstance()
    {
        StopWatch("BenchmarkActivatorCreateInstance_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestStruct = (TestStruct)Activator.CreateInstance(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkActivatorCreateInstance_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestClass = (TestClass)Activator.CreateInstance(typeof(TestClass));
            }
        });
    }

    private void BenchmarkExpression()
    {
        StopWatch("BenchmarkExpression_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestStruct = (TestStruct)ExpressionCreateInstace(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkExpression_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestClass = (TestClass)ExpressionCreateInstace(typeof(TestClass));
            }
        });
    }

    private void BenchmarkExpression2()
    {
        StopWatch("BenchmarkExpression2_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestStruct = (TestStruct)ExpressionCreateInstace2(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkExpression2_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestClass = (TestClass)ExpressionCreateInstace2(typeof(TestClass));
            }
        });
    }

    private void BenchmarkIL()
    {
        StopWatch("BenchmarkIL_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestStruct = (TestStruct)ILCreateInstance(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkIL_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < benchmarkLoopCount; ++i)
            {
                tempTestClass = (TestClass)ILCreateInstance(typeof(TestClass));
            }
        });
    }

    public object ExpressionCreateInstace(Type type)
    {
        if (!expressionCreateInstanceFuncs.ContainsKey(type.FullName))
            if (type.IsValueType)
                expressionCreateInstanceFuncs.Add(type.FullName, Expression.Lambda<ObjectActivator>(Expression.Convert(Expression.New(type), typeof(object))).Compile());
            else
                expressionCreateInstanceFuncs.Add(type.FullName, Expression.Lambda<ObjectActivator>(Expression.New(type)).Compile());
        return expressionCreateInstanceFuncs[type.FullName].Invoke();
    }

    public object ExpressionCreateInstace2(Type type)
    {
        if (!expressionCreateInstanceFuncs2.ContainsKey(type.FullName))
            if (type.IsValueType)
                expressionCreateInstanceFuncs2.Add(type.FullName, Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(type), typeof(object))).Compile());
            else
                expressionCreateInstanceFuncs2.Add(type.FullName, Expression.Lambda<Func<object>>(Expression.New(type)).Compile());
        return expressionCreateInstanceFuncs2[type.FullName].Invoke();
    }

    private object ILCreateInstance(Type type)
    {
        if (!ilCreateInstanceFuncs.ContainsKey(type.FullName))
        {
            var method = new DynamicMethod("", typeof(object), Type.EmptyTypes);
            var il = method.GetILGenerator();

            if (type.IsValueType)
            {
                var local = il.DeclareLocal(type);
                // method.InitLocals == true, so we don't have to use initobj here
                il.Emit(OpCodes.Ldloc, local);
                il.Emit(OpCodes.Box, type);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                var ctor = type.GetConstructor(Type.EmptyTypes);
                il.Emit(OpCodes.Newobj, ctor);
                il.Emit(OpCodes.Ret);
            }
            ilCreateInstanceFuncs.Add(type.FullName, method);
        }
        return ilCreateInstanceFuncs[type.FullName].Invoke(null, null);
    }

    private void StopWatch(string tag, Action action)
    {
        Stopwatch stopWatch = Stopwatch.StartNew();
        action.Invoke();
        stopWatch.Stop();
        UnityEngine.Debug.Log("[" + tag + "] " + stopWatch.Elapsed);
    }
}
