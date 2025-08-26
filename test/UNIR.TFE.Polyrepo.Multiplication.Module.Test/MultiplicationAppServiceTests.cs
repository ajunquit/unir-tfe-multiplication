using UNIR.TFE.Polyrepo.Multiplication.Module.Application;

namespace UNIR.TFE.Polyrepo.Multiplication.Module.Test
{
    public class MultiplicationAppServiceTests
    {
        private readonly MultiplicationAppService _sut;

        // Para llegar a ~13k totales (ajusta si tu base varía)
        private const int BULK_CASES_COUNT = 9435;

        public MultiplicationAppServiceTests()
        {
            _sut = new MultiplicationAppService();
        }

        [Fact]
        public void Key_ShouldReturn_Mul_WhenRequested()
        {
            const string expectedKey = "mul"; // cambia si usas "multiply"
            var actualKey = _sut.Key;
            Assert.Equal(expectedKey, actualKey);
        }

        // ---------------------------
        // Helpers
        // ---------------------------
        private static void AssertNearlyEqual(decimal expected, decimal actual, int decimals = 18)
        {
            var e = decimal.Round(expected, decimals, MidpointRounding.ToEven);
            var a = decimal.Round(actual, decimals, MidpointRounding.ToEven);
            Assert.Equal(e, a);
        }

        private static decimal NextDecimal(Random rng)
        {
            var sign = rng.Next(0, 2) == 0 ? 1m : -1m;
            int magnitude = rng.Next(0, 10_000_000); // 0..9,999,999
            int scale = rng.Next(0, 4);              // 0..3 decimales
            decimal divisor = 1m;
            for (int i = 0; i < scale; i++) divisor *= 10m;
            return sign * (magnitude / divisor);     // rango aprox [-9999.999, 9999.999]
        }

        // ---------------------------
        // Básicas / extremos
        // ---------------------------
        [Theory]
        [InlineData(1, 2, 2)]
        [InlineData(-1, 1, -1)]
        [InlineData(0, 100, 0)]
        [InlineData(123.45, 54.55, 6734.1975)]
        [InlineData(-5, -7, 35)]
        [InlineData(999999999, 1, 999999999)]
        public void Execute_ShouldReturn_CorrectProduct_ForGivenOperands(decimal a, decimal b, decimal expected)
        {
            var actual = _sut.Execute(a, b);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Execute_ShouldHandle_MaxAndMin_WithIdentity()
        {
            Assert.Equal(decimal.MaxValue, _sut.Execute(decimal.MaxValue, 1));
            Assert.Equal(decimal.MinValue, _sut.Execute(decimal.MinValue, 1));
        }

        [Fact]
        public void Execute_WithZeroOperands_ShouldReturnZero()
        {
            Assert.Equal(0m, _sut.Execute(decimal.MaxValue, 0));
            Assert.Equal(0m, _sut.Execute(decimal.MinValue, 0));
            Assert.Equal(0m, _sut.Execute(0, decimal.MaxValue));
            Assert.Equal(0m, _sut.Execute(0, decimal.MinValue));
        }

        // ---------------------------
        // Secuenciales
        // ---------------------------
        public static IEnumerable<object[]> SequentialNumbers()
        {
            for (int i = -500; i <= 500; i++)
            {
                yield return new object[] { i, i, (decimal)i * i };
                yield return new object[] { i, -i, -(decimal)i * i };
                yield return new object[] { i, 0, 0m };
            }
        }

        [Theory]
        [MemberData(nameof(SequentialNumbers))]
        public void Execute_WithSequentialNumbers_ReturnsCorrectResult(decimal a, decimal b, decimal expected)
        {
            var result = _sut.Execute(a, b);
            Assert.Equal(expected, result);
        }

        // ---------------------------
        // Decimales específicos
        // ---------------------------
        public static IEnumerable<object[]> DecimalTestCases()
        {
            var testCases = new[]
            {
                (0.1m, 0.2m, 0.02m),
                (1.111m, 2.222m, 2.468642m),
                (99.99m, 0.01m, 0.9999m),
                (123.456m, 789.012m, 97408.265472m),
                (-45.67m, 45.67m, -2085.7489m),
                (1000.001m, 0.999m, 999.000999m),
                (0.0001m, 0.0001m, 0.00000001m),
                (999.999m, 0.001m, 0.999999m)
            };
            foreach (var (a, b, expected) in testCases)
                yield return new object[] { a, b, expected };
        }

        [Theory]
        [MemberData(nameof(DecimalTestCases))]
        public void Execute_WithDecimalNumbers_ReturnsPreciseProduct(decimal a, decimal b, decimal expected)
        {
            var result = _sut.Execute(a, b);
            Assert.Equal(expected, result);
        }

        // ---------------------------
        // Números grandes (sin overflow)
        // ---------------------------
        public static IEnumerable<object[]> LargeNumbersTestCases()
        {
            var largeNumbers = new[]
            {
                1_000_000m,
                5_000_000m,
                10_000_000m,
                50_000_000m,
                100_000_000m,
                500_000_000m,
                1_000_000_000m,
                5_000_000_000m,
                10_000_000_000m,
                50_000_000_000m
            };
            foreach (var n in largeNumbers)
            {
                yield return new object[] { n, n, n * n };
                yield return new object[] { n, 1m, n };
                yield return new object[] { -n, n, -n * n };
                yield return new object[] { n, 0m, 0m };
            }
        }

        [Theory]
        [MemberData(nameof(LargeNumbersTestCases))]
        public void Execute_WithLargeNumbers_ReturnsCorrectResult(decimal a, decimal b, decimal expected)
        {
            var result = _sut.Execute(a, b);
            Assert.Equal(expected, result);
        }

        // ---------------------------
        // Propiedades matemáticas
        // ---------------------------

        // Conmutativa: a*b = b*a (exacta)
        public static IEnumerable<object[]> CommutativePropertyTestCases()
        {
            var random = new Random(20250825);
            for (int i = 0; i < 100; i++)
            {
                decimal a = (decimal)(random.NextDouble() * 1000 - 500);
                decimal b = (decimal)(random.NextDouble() * 1000 - 500);
                yield return new object[] { a, b };
            }
        }

        [Theory]
        [MemberData(nameof(CommutativePropertyTestCases))]
        public void Execute_ShouldBeCommutative(decimal a, decimal b)
        {
            var ab = _sut.Execute(a, b);
            var ba = _sut.Execute(b, a);
            Assert.Equal(ab, ba);
        }

        // Asociativa: (a*b)*c ≈ a*(b*c)  (comparación con tolerancia por redondeo)
        public static IEnumerable<object[]> AssociativePropertyTestCases()
        {
            var random = new Random(20250826);
            for (int i = 0; i < 100; i++)
            {
                // mantenemos rangos moderados para minimizar redondeos
                decimal a = (decimal)(random.NextDouble() * 100 - 50);
                decimal b = (decimal)(random.NextDouble() * 100 - 50);
                decimal c = (decimal)(random.NextDouble() * 100 - 50);
                yield return new object[] { a, b, c };
            }
        }

        [Theory]
        [MemberData(nameof(AssociativePropertyTestCases))]
        public void Execute_ShouldBeAssociative(decimal a, decimal b, decimal c)
        {
            var left = _sut.Execute(_sut.Execute(a, b), c);
            var right = _sut.Execute(a, _sut.Execute(b, c));
            AssertNearlyEqual(left, right, decimals: 18);
        }

        // Distributiva: a*(b+c) ≈ a*b + a*c (con tolerancia)
        public static IEnumerable<object[]> DistributivePropertyTestCases()
        {
            var random = new Random(20250827);
            for (int i = 0; i < 100; i++)
            {
                decimal a = (decimal)(random.NextDouble() * 200 - 100);
                decimal b = (decimal)(random.NextDouble() * 200 - 100);
                decimal c = (decimal)(random.NextDouble() * 200 - 100);
                yield return new object[] { a, b, c };
            }
        }

        [Theory]
        [MemberData(nameof(DistributivePropertyTestCases))]
        public void Execute_ShouldBeDistributiveOverAddition(decimal a, decimal b, decimal c)
        {
            var left = _sut.Execute(a, b + c);
            var right = _sut.Execute(a, b) + _sut.Execute(a, c);
            AssertNearlyEqual(left, right, decimals: 18);
        }

        // Identidades: a*1=a y a*0=0
        public static IEnumerable<object[]> IdentityElementTestCases()
        {
            var random = new Random(20250828);
            for (int i = 0; i < 100; i++)
            {
                decimal a = (decimal)(random.NextDouble() * 2000 - 1000);
                yield return new object[] { a };
            }
        }

        [Theory]
        [MemberData(nameof(IdentityElementTestCases))]
        public void Execute_WithOne_ShouldReturnSameNumber(decimal a)
        {
            Assert.Equal(a, _sut.Execute(a, 1));
            Assert.Equal(a, _sut.Execute(1, a));
        }

        [Theory]
        [MemberData(nameof(IdentityElementTestCases))]
        public void Execute_WithZero_ShouldReturnZero(decimal a)
        {
            Assert.Equal(0, _sut.Execute(a, 0));
            Assert.Equal(0, _sut.Execute(0, a));
        }

        // Rendimiento / repetición
        [Fact]
        public void Execute_ShouldHandle_MultipleOperationsCorrectly()
        {
            decimal result = 1;
            decimal expected = 1;
            for (int i = 1; i <= 1000; i++)
            {
                result = _sut.Execute(result, -1);
                expected = -expected;
            }
            Assert.Equal(expected, result);
        }

        // Precisión (casos exactos/terminantes)
        public static IEnumerable<object[]> PrecisionTestCases()
        {
            return new[]
            {
                new object[] { 0.00000001m, 0.00000001m, 0.0000000000000001m }, // 1e-8 * 1e-8
                new object[] { 1.2345m, 2.3456m, 2.89564320m },
                new object[] { 123456789.123456789m, 0.000000001m, 0.123456789123456789m },
                new object[] { 2500m, 0.004m, 10.000m },
                new object[] { 10000000000000000000000000000m, 0.0000000000000000000000000001m, 1.0000000000000000000000000000m }
            };
        }

        [Theory]
        [MemberData(nameof(PrecisionTestCases))]
        public void Execute_WithHighPrecisionNumbers_MaintainsPrecision(decimal a, decimal b, decimal expected)
        {
            var result = _sut.Execute(a, b);
            Assert.Equal(expected, result);
        }

        // ---------------------------
        // Dataset masivo (~9435 casos)
        // ---------------------------
        public static IEnumerable<object[]> BulkMultiplicationCases()
        {
            var rng = new Random(424242); // seed fija
            for (int i = 0; i < BULK_CASES_COUNT; i++)
            {
                decimal a = NextDecimal(rng);
                decimal b = NextDecimal(rng);
                decimal expected = a * b;
                yield return new object[] { a, b, expected };
            }
        }

        [Trait("size", "bulk")]
        [Theory]
        [MemberData(nameof(BulkMultiplicationCases))]
        public void Execute_BulkRandomizedDataset_ReturnsCorrectProduct(decimal a, decimal b, decimal expected)
        {
            var result = _sut.Execute(a, b);
            Assert.Equal(expected, result);
        }
    }
}
