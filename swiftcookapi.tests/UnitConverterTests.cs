using SwiftCookDb.Models;
using swiftcookapi.Services;
using Xunit;

namespace swiftcookapi.tests
{
    public class UnitConverterTests
    {
        private static Unit U(int id, string name, UnitDimension d, decimal? f) =>
            new() { Id = id, Name = name, Dimension = d, ToBaseFactor = f };

        private static readonly Unit Gram = U(2, "gram", UnitDimension.Mass, 1);
        private static readonly Unit Kg = U(3, "kilogram", UnitDimension.Mass, 1000);
        private static readonly Unit Oz = U(5, "ounce", UnitDimension.Mass, 28.349523m);
        private static readonly Unit Ml = U(7, "milliliter", UnitDimension.Volume, 1);
        private static readonly Unit Litre = U(8, "liter", UnitDimension.Volume, 1000);
        private static readonly Unit Tbsp = U(10, "tablespoon", UnitDimension.Volume, 15);
        private static readonly Unit Cup = U(11, "cup", UnitDimension.Volume, 250);
        private static readonly Unit FlOz = U(17, "fluid ounce", UnitDimension.Volume, 28.413063m);
        private static readonly Unit Sgl = U(1, "sgl", UnitDimension.Count, null);
        private static readonly Unit Clove = U(14, "clove", UnitDimension.Count, null);
        private static readonly Unit Pinch = U(15, "pinch", UnitDimension.Other, null);

        private readonly UnitConverter _c = new();

        [Fact]
        public void ConvertsWithinMassAndVolume()
        {
            Assert.True(_c.TryConvert(0.5m, Kg, Gram, out var g));
            Assert.Equal(500m, g);
            Assert.True(_c.TryConvert(2m, Cup, Tbsp, out var tbsp));
            Assert.Equal(33.333333333333333333333333333m, tbsp, 6);
            Assert.True(_c.TryConvert(1m, Oz, Gram, out var oz));
            Assert.Equal(28.349523m, oz);
        }

        [Fact]
        public void WeightOunceAndFluidOunceAreNotInterchangeable()
        {
            Assert.False(_c.CanConvert(Oz, FlOz));
        }

        [Fact]
        public void NeverConvertsAcrossDimensionsOrBetweenDistinctCountUnits()
        {
            Assert.False(_c.CanConvert(Gram, Cup));
            Assert.False(_c.CanConvert(Sgl, Clove));
            Assert.False(_c.CanConvert(Pinch, Gram));
            Assert.False(_c.TryConvert(1m, Gram, Cup, out _));
        }

        [Fact]
        public void SameCountOrOtherUnitIsComparable()
        {
            Assert.True(_c.TryConvert(3m, Clove, Clove, out var r));
            Assert.Equal(3m, r);
            Assert.True(_c.CanConvert(Pinch, Pinch));
        }

        [Fact]
        public void AddsInFirstUnitAndRefusesIncomparable()
        {
            Assert.True(_c.TryAdd(200m, Gram, 0.5m, Kg, out var sum));
            Assert.Equal(700m, sum);
            Assert.False(_c.TryAdd(1m, Gram, 1m, Cup, out _));
        }

        [Fact]
        public void ComparesAcrossUnitsOfTheSameDimension()
        {
            Assert.Equal(-1, _c.Compare(200m, Gram, 0.5m, Kg));
            Assert.Equal(0, _c.Compare(1m, Kg, 1000m, Gram));
            Assert.Null(_c.Compare(1m, Gram, 1m, Ml));
        }

        [Fact]
        public void ToDisplayPicksLargestUnitAtLeastOne()
        {
            var metricMass = new[] { Gram, Kg };
            Assert.Equal((1.5m, Kg), _c.ToDisplay(1500m, Gram, metricMass));
            Assert.Equal((250m, Gram), _c.ToDisplay(250m, Gram, metricMass));
            Assert.Equal((2m, Litre), _c.ToDisplay(2000m, Ml, new[] { Ml, Litre }));
        }

        [Fact]
        public void ToDisplayFallsBackToSmallestUnitBelowOne()
        {
            var (amount, unit) = _c.ToDisplay(0.25m, Gram, new[] { Gram, Kg });
            Assert.Equal(0.25m, amount);
            Assert.Equal(Gram, unit);
        }

        [Fact]
        public void ToDisplayLeavesUnconvertibleAmountsUnchanged()
        {
            Assert.Equal((3m, Clove), _c.ToDisplay(3m, Clove, new[] { Gram, Kg }));
            Assert.Equal((1m, Cup), _c.ToDisplay(1m, Cup, new[] { Gram, Kg }));
        }
    }
}
