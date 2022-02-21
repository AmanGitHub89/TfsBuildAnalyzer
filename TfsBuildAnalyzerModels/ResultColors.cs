using System.Windows.Media;

namespace TfsBuildAnalyzerModels
{
    public class ResultColors
    {
        public static readonly string VNextBuild = "#D3D3D3";
        public static readonly string XamlBuild = "#A9A9A9";

        public static readonly string Passed = "#90EE90";
        public static readonly string Failed = "#FD5454";
        public static readonly string NewFailure = "#FF0000";
        public static readonly string Ignored = "#FFFF00";
        public static readonly string ExcelHeader = "#7B7B7B";

        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush VNextBuildColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(VNextBuild));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush XamlBuildColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(XamlBuild));

        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush PassedColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Passed));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush FailedColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Failed));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush NewFailureColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(NewFailure));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush IgnoredColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(Ignored));
        // ReSharper disable once PossibleNullReferenceException
        public static SolidColorBrush ExcelHeaderColor => new SolidColorBrush((Color)ColorConverter.ConvertFromString(ExcelHeader));
    }
}
