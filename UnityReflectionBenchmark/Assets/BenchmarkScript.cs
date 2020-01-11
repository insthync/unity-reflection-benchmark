using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
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

    void Start()
    {
        BenchmarkIL();
        BenchmarkExpression();
        BenchmarkActivatorCreateInstance();
    }

    private void BenchmarkActivatorCreateInstance()
    {
        StopWatch("BenchmarkActivatorCreateInstance_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < 10000; ++i)
            {
                tempTestStruct = (TestStruct)Activator.CreateInstance(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkActivatorCreateInstance_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < 10000; ++i)
            {
                tempTestClass = (TestClass)Activator.CreateInstance(typeof(TestClass));
            }
        });
    }

    private void BenchmarkExpression()
    {
        StopWatch("BenchmarkExpression_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < 10000; ++i)
            {
                tempTestStruct = (TestStruct)ExpressionCreateInstace(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkExpression_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < 10000; ++i)
            {
                tempTestClass = (TestClass)ExpressionCreateInstace(typeof(TestClass));
            }
        });
    }

    private void BenchmarkIL()
    {
        StopWatch("BenchmarkIL_Struct", () => {
            TestStruct tempTestStruct;
            for (int i = 0; i < 10000; ++i)
            {
                tempTestStruct = (TestStruct)ILCreateInstance(typeof(TestStruct));
            }
        });
        StopWatch("BenchmarkIL_Class", () =>
        {
            TestClass tempTestClass;
            for (int i = 0; i < 10000; ++i)
            {
                tempTestClass = (TestClass)ILCreateInstance(typeof(TestClass));
            }
        });
    }

    public object ExpressionCreateInstace(Type type)
    {
        if (type.IsValueType)
            return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(type), typeof(object))).Compile().Invoke();
        return Expression.Lambda<Func<object>>(Expression.New(type)).Compile().Invoke();
    }

    private object ILCreateInstance(Type type)
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

        return method.Invoke(null, null);
    }

    private void StopWatch(string tag, Action action)
    {
        Stopwatch stopWatch = Stopwatch.StartNew();
        action.Invoke();
        stopWatch.Stop();
        UnityEngine.Debug.Log("[" + tag + "] " + stopWatch.Elapsed);
    }
}
