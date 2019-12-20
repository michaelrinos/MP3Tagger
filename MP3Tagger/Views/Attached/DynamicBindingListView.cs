using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows;
using System.Collections;
using System.Windows.Data;
using MP3Tagger.Converters;

namespace MP3Tagger.Views.Attached
{
    public class DynamicBindingListView
    {
 
        public static bool GetGenerateColumnsGridView(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
 
            return (bool)element.GetValue(GenerateColumnsGridViewProperty);
        }
 
        public static void SetGenerateColumnsGridView(DependencyObject element, bool value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
 
            element.SetValue(GenerateColumnsGridViewProperty, value);
        }

        public static void SetInnerProperty(DependencyObject element, string value) {
            if (value == null) {
                throw new ArgumentNullException("element");
            }
            element.SetValue(InnerPropertyProperty, value);
        }
        public static string GetInnerProperty(DependencyObject dependencyObject) {
            return (string)dependencyObject.GetValue(InnerPropertyProperty);
        }


        public static readonly DependencyProperty InnerPropertyProperty = DependencyProperty.RegisterAttached("InnerProperty", typeof(string), typeof(DynamicBindingListView), new PropertyMetadata(string.Empty)); 
        public static readonly DependencyProperty GenerateColumnsGridViewProperty = DependencyProperty.RegisterAttached("GenerateColumnsGridView", typeof(bool?), typeof(DynamicBindingListView), new FrameworkPropertyMetadata(null, thePropChanged));
 
 
        public static string GetDateFormatString(DependencyObject element)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
 
            return (string)element.GetValue(DateFormatStringProperty);
        }
 
        public static void SetDateFormatString(DependencyObject element, string value)
        {
            if (element == null)
            {
                throw new ArgumentNullException("element");
            }
 
            element.SetValue(DateFormatStringProperty, value);
        }
 
 
        public static readonly DependencyProperty DateFormatStringProperty = DependencyProperty.RegisterAttached("DateFormatString", typeof(string), typeof(DynamicBindingListView), new FrameworkPropertyMetadata(null));
        public static void thePropChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ListView lv = (ListView)obj;
            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(ListView.ItemsSourceProperty, typeof(ListView));
            descriptor.AddValueChanged(lv, new EventHandler(ItemsSourceChanged));
        }
 
        private static void ItemsSourceChanged(object sender, EventArgs e)
        {
            ListView lv = (ListView)sender;
            IEnumerable its = lv.ItemsSource;
            IEnumerator itsEnumerator = its.GetEnumerator();
            bool hasItems = itsEnumerator.MoveNext();
            if (hasItems)
            {
                SetUpTheColumns(lv, itsEnumerator.Current);
            }
        }

        private static void SetUpTheColumns(ListView theListView, object firstObject) {
            PropertyInfo[] theClassProperties;
            if (String.IsNullOrEmpty(GetInnerProperty(theListView))) {
                theClassProperties = firstObject.GetType().GetProperties();
            } else {
                var innerProperty = firstObject.GetType().GetProperty(GetInnerProperty(theListView))?.GetValue(firstObject);
                theClassProperties = innerProperty?.GetType().GetProperties();
                firstObject = (object)innerProperty;
                
            }
            GridView gv = (GridView)theListView.View;
            foreach (PropertyInfo pi in theClassProperties)
            {
                string columnName = pi.Name;
                GridViewColumn grv = new GridViewColumn { Header = columnName };

                if (object.ReferenceEquals(pi.PropertyType, typeof(DateTime))) {
                    Binding bnd = new Binding(columnName);
                    string formatString = (string)theListView.GetValue(DateFormatStringProperty);
                    if (formatString != string.Empty) {
                        bnd.StringFormat = formatString;
                    }
                    BindingOperations.SetBinding(grv, TextBlock.TextProperty, bnd);
                    grv.DisplayMemberBinding = bnd;
                } else {
                    Binding bnd = new Binding(columnName);
                    bnd.Path = new PropertyPath(GetInnerProperty(theListView) + "." + pi.Name);
                    if (firstObject.GetType().GetProperty(columnName).GetValue(firstObject, null) is Array) {
                        bnd.Converter = new ArrayValuesToString();
                    }
                    BindingOperations.SetBinding(grv, TextBlock.TextProperty, bnd);
                    grv.DisplayMemberBinding = bnd;
                }
                gv.Columns.Add(grv);
            }
        }
 
    }
}