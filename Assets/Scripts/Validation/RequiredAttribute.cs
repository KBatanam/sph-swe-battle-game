using System;
using UnityEngine;

namespace Validation
{
    /// <summary>
    /// InspectorからのUnityオブジェクト参照設定が必須であることを表す。
    /// 実行時のnull検証は別途行う必要がある。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RequiredAttribute : PropertyAttribute
    {
    }
}
