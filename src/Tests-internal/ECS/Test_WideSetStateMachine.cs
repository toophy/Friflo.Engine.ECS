using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Wide = Friflo.Engine.ECS.Utils.WideBitSet;

namespace Internal.ECS
{
public static class Test_WideSetStateMachine
{
    [TestCase(17)] [TestCase(101)] [TestCase(8191)]
    public static void ContinuousOperationsAndAliasesMatchIndependentSets(int seed)
    {
        var random = new Random(seed);
        var actual = new Wide[3];
        var expected = new HashSet<int>[3];
        int[] boundaries = { 0, 63, 64, 127, 128, 255, 256, 257, 511, 512, 513, 65534, 65535 };
        for (int n = 0; n < 3; n++) {
            expected[n] = new HashSet<int>(boundaries.Where(bit => bit % 3 == n));
            actual[n] = new Wide(expected[n].ToArray());
        }
        var trace = new StringBuilder();
        for (int step = 0; step < 500; step++) {
            int left = random.Next(3);
            int right = random.Next(3);
            int operation = random.Next(7);
            int bit = random.Next(2) == 0 ? boundaries[random.Next(boundaries.Length)] : random.Next(65536);
            trace.AppendLine($"{step}: op={operation} left={left} right={right} bit={bit}");
            try {
                var snapshot = actual[left];
                var expectedSnapshot = new HashSet<int>(expected[left]);
                var operand = actual[right];
                var expectedOperand = new HashSet<int>(expected[right]);
                switch (operation) {
                    case 0: actual[left].SetBit(bit); expected[left].Add(bit); break;
                    case 1: actual[left].ClearBit(bit); expected[left].Remove(bit); break;
                    case 2: actual[left].Add(operand); expected[left].UnionWith(expectedOperand); break;
                    case 3: actual[left].Remove(operand); expected[left].ExceptWith(expectedOperand); break;
                    case 4: actual[left] = Wide.Changed(snapshot, operand); expected[left].SymmetricExceptWith(expectedOperand); break;
                    case 5: actual[left] = Wide.Intersect(snapshot, operand); expected[left].IntersectWith(expectedOperand); break;
                    case 6: actual[left] = operand; expected[left] = expectedOperand; break;
                }
                AssertSet(snapshot, expectedSnapshot);
                AssertSet(operand, expectedOperand);
                for (int n = 0; n < actual.Length; n++) AssertSet(actual[n], expected[n]);
                var added = new HashSet<int>(expectedOperand);
                added.ExceptWith(expectedSnapshot);
                AssertSet(Wide.Added(snapshot, operand), added);
                var removed = new HashSet<int>(expectedSnapshot);
                removed.ExceptWith(expectedOperand);
                AssertSet(Wide.Removed(snapshot, operand), removed);
                Assert.AreEqual(expectedSnapshot.IsSupersetOf(expectedOperand), snapshot.HasAll(operand));
                Assert.AreEqual(expectedSnapshot.Overlaps(expectedOperand), snapshot.HasAny(operand));
            } catch (Exception error) {
                Assert.Fail($"seed={seed}, step={step}\n{trace}\n{error}");
            }
        }
    }

    private static void AssertSet(Wide actual, HashSet<int> expected)
    {
        var bits = new List<int>();
        foreach (int bit in actual) bits.Add(bit);
        Assert.AreEqual(expected.Count, actual.GetBitCount());
        CollectionAssert.AreEqual(expected.OrderBy(bit => bit), bits);
        var rebuilt = new Wide(expected.OrderByDescending(bit => bit).ToArray());
        Assert.IsTrue(actual.Equals(rebuilt));
        Assert.AreEqual(actual.HashCode(), rebuilt.HashCode());
    }
}
}
