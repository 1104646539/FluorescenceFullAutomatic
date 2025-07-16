using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FluorescenceFullAutomatic.Platform.Attached
{
    /// <summary>
    /// TextBox附加属性
    /// </summary>
    public class TextBoxAttachedProperties
    {
 
            public static string GetRegexPattern(DependencyObject obj)
            {
                return (string)obj.GetValue(RegexPatternProperty);
            }

            public static void SetRegexPattern(DependencyObject obj, string value)
            {
                obj.SetValue(RegexPatternProperty, value);
            }

            public static readonly DependencyProperty RegexPatternProperty = DependencyProperty.RegisterAttached(
                    "RegexPattern",
                    typeof(string),
                    typeof(TextBoxAttachedProperties),
                    new PropertyMetadata(string.Empty, OnRegexPatternChanged));

            private static void OnRegexPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            {
                if (d is TextBox textBox)
                {
                    string value = (string)e.NewValue;
                    if (!string.IsNullOrEmpty(value))
                    {
                        textBox.PreviewTextInput += TextBox_PreviewTextInput;
                        textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                        textBox.TextChanged += TextBox_TextChanged;
                        DataObject.AddPastingHandler(textBox, TextBox_PastingEvent);
                    }
                    else
                    {
                        textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                        textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                        textBox.TextChanged -= TextBox_TextChanged;
                        DataObject.RemovePastingHandler(textBox, TextBox_PastingEvent);
                    }
                }
            }

            private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
            {
                //TODO 相关业务处理
                // ...
            }

            private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
            {
                //TODO 相关业务处理
                // ...

                //禁止使用 空格键
                //if (e.Key == Key.Space)
                //{
                //    e.Handled = true;
                //}
            }

            private static void TextBox_PastingEvent(object sender, DataObjectPastingEventArgs e)
            {
                //TODO 相关业务处理
                // ...

                //取消 粘贴命令
                //e.CancelCommand();
            }

            private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
            {
                if (sender is TextBox textBox)
                {
                    Regex regex = new Regex(GetRegexPattern(textBox));
                    //e.Handled = !regex.IsMatch(e.Text);
                    e.Handled = !regex.IsMatch(textBox.Text.Insert(textBox.SelectionStart, e.Text));
                }
            }
        }


}
