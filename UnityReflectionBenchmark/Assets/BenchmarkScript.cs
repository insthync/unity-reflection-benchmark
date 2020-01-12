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
    private readonly Dictionary<string, DynamicMethod> ilCreateInstanceFuncs = new Dictionary<string, DynamicMethod>();

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
        BenchmarkActivatorCreateInstance();
        BenchmarkMethodInfoInvoke();
        BenchmarkDelegateDynamicInvoke();
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
