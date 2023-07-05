/*header
    > File Name: TestSIMD.cs
    > Create Time: 2023-07-04 星期二 11时52分52秒
    > Author: treertzhu
*/

/*
单指令多数据，向量加速。64位专用。
https://learn.microsoft.com/en-us/dotnet/standard/simd

Remarks
SIMD is more likely to remove one bottleneck and expose the next, for example memory throughput.
In general the performance benefit of using SIMD varies depending on the specific scenario,
and in some cases it can even perform worse than simpler non-SIMD equivalent code.
*/

using System.Numerics;

namespace MyTest;

public static class SIMD
{
    public static void Run()
    {
        Console.WriteLine($"Vector.IsHardwareAccelerated = {Vector.IsHardwareAccelerated}");
        {
            var v1 = new Vector2(0.1f, 0.2f);
            var v2 = new Vector2(1.1f, 2.2f);
            var vResult = v1 + v2;
            var vResult1 = Vector2.Dot(v1, v2);
            var vResult2 = Vector2.Distance(v1, v2);
            var vResult3 = Vector2.Clamp(v1, Vector2.Zero, Vector2.One);
            Console.WriteLine($"{vResult} {vResult1} {vResult2} {vResult3}");
        }
        {
            var m1 = new Matrix4x4(
            1.1f, 1.2f, 1.3f, 1.4f,
            2.1f, 2.2f, 3.3f, 4.4f,
            3.1f, 3.2f, 3.3f, 3.4f,
            4.1f, 4.2f, 4.3f, 4.4f);

            var m2 = Matrix4x4.Transpose(m1);
            var mResult = Matrix4x4.Multiply(m1, m2);
            Console.WriteLine($"{m2} {mResult}");
        }
    }
}