using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Remotemonitor.Converters;
using System.Globalization;
using System.Windows.Media;
using Xunit;

namespace RemoteMonitor.Tests
{
    public class ConvertersTests
    {
        [Theory]
        [InlineData(true, "Keine Daten (> 30s)")]
        [InlineData(false, "OK")]
        public void BoolToStatusConverter_Convert_ReturnsExpectedString(bool input, string expected)
        {
            var c = new BoolToStatusConverter();

            var result = c.Convert(input, typeof(string), parameter: null!, culture: CultureInfo.InvariantCulture);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null, "OK")]     // value is bool b && b => false
        [InlineData("x", "OK")]      // nicht bool => false
        [InlineData(1, "OK")]        // nicht bool => false
        public void BoolToStatusConverter_Convert_NonBool_TreatedAsFalse(object input, string expected)
        {
            var c = new BoolToStatusConverter();

            var result = c.Convert(input, typeof(string), null!, CultureInfo.InvariantCulture);

            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(true, typeof(SolidColorBrush), nameof(Colors.Red))]
        [InlineData(false, typeof(SolidColorBrush), nameof(Colors.Lime))]
        public void BoolToColorConverter_Convert_ReturnsBrushWithExpectedColor(
            bool input, Type expectedType, string expectedColorName)
        {
            var c = new BoolToColorConverter();

            var result = c.Convert(input, typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

            Assert.IsType(expectedType, result);

            var brush = (SolidColorBrush)result;
            var expectedColor = (Color)typeof(Colors).GetProperty(expectedColorName)!.GetValue(null)!;

            Assert.Equal(expectedColor, brush.Color);
        }

        [Fact]
        public void BoolToColorConverter_Convert_NonBool_TreatedAsFalse_Lime()
        {
            var c = new BoolToColorConverter();

            var result = c.Convert("not-bool", typeof(SolidColorBrush), null!, CultureInfo.InvariantCulture);

            var brush = Assert.IsType<SolidColorBrush>(result);
            Assert.Equal(Colors.Lime, brush.Color);
        }

        [Fact]
        public void ConvertBack_ThrowsNotImplemented()
        {
            var c1 = new BoolToStatusConverter();
            var c2 = new BoolToColorConverter();

            Assert.Throws<NotImplementedException>(() => c1.ConvertBack("x", typeof(bool), null!, CultureInfo.InvariantCulture));
            Assert.Throws<NotImplementedException>(() => c2.ConvertBack("x", typeof(bool), null!, CultureInfo.InvariantCulture));
        }
    }
}
