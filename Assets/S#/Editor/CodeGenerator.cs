using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CodeGenerator
{
    [MenuItem("Window/SPGen")]
    public static void Gen()
    {
        // プロジェクトに置いてあるastGenを使って、swiftからast jsonを吐き出す -> jsonからidentifierを抜き出す -> C#コードを吐き出す とかできると良い。

        var str = string.Empty;
        using (var sr = new StreamReader("Assets/Libraries/ReplayKitWrapper/Plugins/iOS/ReplayKitSwift.json"))
        {
            str = sr.ReadToEnd();
        }

        var obj = MiniJSONForS_Sharp.Json.Deserialize(str);
        var classInfos = new List<ClassInfo>();

        // 収集
        CollectTargetDecl(obj, classInfos);

        foreach (var classInfo in classInfos)
        {
            Debug.Log("classInfo:" + classInfo.className);
            foreach (var func in classInfo.funcs)
            {
                Debug.Log("methodInfo:" + func.funcName);
            }
        }
    }

    private class ClassInfo
    {
        public readonly string className;
        public List<FuncInfo> funcs = new List<FuncInfo>();
        public ClassInfo(string className)
        {
            this.className = className;
        }

        public class FuncInfo
        {
            public readonly string funcName;
            public FuncInfo(string funcName)
            {
                this.funcName = funcName;
            }
        }
    }

    private enum Target
    {
        None,
        AtFound,
        ObjcFound,
        ClassFound,
        FuncFound
    }
    private static Target lockEnum = Target.None;
    private static ClassInfo currentClass;

    static void CollectTargetDecl(object obj, List<ClassInfo> classList)
    {
        var list = obj as List<object>;
        foreach (var a in list)
        {
            switch (a.ToString())
            {
                case "System.Collections.Generic.Dictionary`2[System.String,System.Object]":
                    {
                        var d = a as Dictionary<string, object>;
                        foreach (var i in d)
                        {
                            // Debug.Log("i:" + i.Key + " v:" + i.Value);
                            var strVal = i.Value as string;
                            if (strVal == null)
                            {
                                continue;
                            }
                            switch (strVal)
                            {
                                case "@":
                                    {
                                        if (lockEnum == Target.None)
                                        {
                                            lockEnum = Target.AtFound;
                                        }
                                        break;
                                    }
                                case "objc":
                                    {
                                        if (lockEnum != Target.AtFound)
                                        {
                                            lockEnum = Target.None;
                                            break;
                                        }
                                        lockEnum = Target.ObjcFound;

                                        break;
                                    }
                                case "class":
                                    {
                                        if (lockEnum != Target.ObjcFound)
                                        {
                                            lockEnum = Target.None;
                                            break;
                                        }
                                        lockEnum = Target.ClassFound;

                                        break;
                                    }
                                case "func":
                                    {
                                        if (lockEnum != Target.ObjcFound)
                                        {
                                            lockEnum = Target.None;
                                            break;
                                        }
                                        lockEnum = Target.FuncFound;
                                        break;
                                    }
                                // classの次の要素を拾いたい
                                default:
                                    {
                                        if (lockEnum == Target.ClassFound)
                                        {
                                            currentClass = new ClassInfo(strVal);
                                            classList.Add(currentClass);
                                            lockEnum = Target.None;
                                            break;
                                        }
                                        if (lockEnum == Target.FuncFound)
                                        {
                                            currentClass.funcs.Add(new ClassInfo.FuncInfo(strVal));
                                            lockEnum = Target.None;
                                            break;
                                        }

                                        // Debug.Log("e " + strVal + " lockEnum:" + lockEnum);

                                        break;
                                    }
                            }
                        }

                        if (d.ContainsKey("children"))
                        {
                            var childlen = d["children"] as List<object>;
                            CollectTargetDecl(childlen, classList);
                        }
                        break;
                    }
                default:
                    {
                        Debug.Log("a.ToString():" + a.ToString());
                        break;
                    }
            }

            // var d = a as Dictionary<string, string>;
            // foreach (var i in d)
            // {

            // }
        }
    }
}