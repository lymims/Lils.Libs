﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Lils.WpfLib.Tools;

namespace Lils.WpfLib.Controls
{
    /// <summary>
    /// A slider can fill with content
    /// </summary>
    [TemplatePart(Name = "PART_CubeViewport", Type = typeof(Viewport3D))]
    [TemplatePart(Name = "PART_MoveYTranslate", Type = typeof(TranslateTransform3D))]
    [TemplatePart(Name = "PART_ContentGrid", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_Pointer", Type = typeof(UIElement))]
    public class ContentCube : Control
    {
        static ContentCube()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ContentCube), new FrameworkPropertyMetadata(typeof(ContentCube)));
        }

        private Viewport3D cubeViewport3D;
        TranslateTransform3D moveYTranslate;

        private Grid contentGrid;
        private UIElement pointer;

        public ContentCube()
        {
        }

        public object XYContent
        {
            get { return (object)GetValue(XYContentProperty); }
            set { SetValue(XYContentProperty, value); }
        }

        public object ZYContent
        {
            get { return (object)GetValue(ZYContentProperty); }
            set { SetValue(ZYContentProperty, value); }
        }

        public object XZContent
        {
            get { return (object)GetValue(XZContentProperty); }
            set { SetValue(XZContentProperty, value); }
        }

        public double YOffset
        {
            get { return (double)GetValue(YOffsetProperty); }
            set { SetValue(YOffsetProperty, value); }
        }

        public object LayerContent
        {
            get { return (object)GetValue(LayerContentProperty); }
            set { SetValue(LayerContentProperty, value); }
        }

        public Point3D Maximum
        {
            get { return (Point3D)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public Point3D Minimum
        {
            get { return (Point3D)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public Point3D Value
        {
            get { return (Point3D)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public object Pointer
        {
            get { return (object)GetValue(PointerProperty); }
            set { SetValue(PointerProperty, value); }
        }

        public static readonly DependencyProperty XYContentProperty =
            DependencyProperty.Register("XYContent", typeof(object), typeof(ContentCube), new PropertyMetadata(null));

        public static readonly DependencyProperty ZYContentProperty =
            DependencyProperty.Register("ZYContent", typeof(object), typeof(ContentCube), new PropertyMetadata(null));

        public static readonly DependencyProperty XZContentProperty =
            DependencyProperty.Register("XZContent", typeof(object), typeof(ContentCube), new PropertyMetadata(null));

        public static readonly DependencyProperty LayerContentProperty =
            DependencyProperty.Register("LayerContent", typeof(object), typeof(ContentCube), new PropertyMetadata(null));

        public static readonly DependencyProperty YOffsetProperty =
            DependencyProperty.Register("YOffset", typeof(double), typeof(ContentCube), new PropertyMetadata(0.0, YOffsetPropertyChangedCallback, CoerceYOffsetPropertyCallback));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(Point3D), typeof(ContentCube), new PropertyMetadata(new Point3D(1, 1, 1)));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(Point3D), typeof(ContentCube), new PropertyMetadata(new Point3D(0, 0, 0)));

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(Point3D), typeof(ContentCube), new PropertyMetadata(new Point3D(0, 0, 0)));

        public static readonly DependencyProperty PointerProperty =
            DependencyProperty.Register("Pointer", typeof(object), typeof(ContentCube), new PropertyMetadata(null));

        private static void YOffsetPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContentCube cube && cube.moveYTranslate != null)
            {
                cube.moveYTranslate.OffsetY = (double)e.NewValue * 10;
            }
        }

        private static object CoerceYOffsetPropertyCallback(DependencyObject d, object baseValue)
        {
            if ((double)baseValue < 0)
            {
                return 0.0;
            }
            else if ((double)baseValue > 1)
            {
                return 1.0;
            }
            else
            {
                return baseValue;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            moveYTranslate = GetTemplateChild("PART_MoveYTranslate") as TranslateTransform3D;

            cubeViewport3D = GetTemplateChild("PART_CubeViewport") as Viewport3D;

            cubeViewport3D.MouseDown += CubeViewport3D_MouseAction;
            cubeViewport3D.MouseMove += CubeViewport3D_MouseAction;

            cubeViewport3D.MouseUp += (s, e) =>
            {
                previousPosition = null;
                Mouse.Capture(null);
            };

            contentGrid = GetTemplateChild("PART_ContentGrid") as Grid;
            contentGrid.MouseDown += ContentGrid_MouseAction;
            contentGrid.MouseMove += ContentGrid_MouseAction;
            contentGrid.MouseUp += ContentGrid_MouseUp;

            pointer = GetTemplateChild("PART_Pointer") as UIElement;
        }

        private Point? previousPosition;

        private void CubeViewport3D_MouseAction(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            var currentPosition = e.GetPosition(cubeViewport3D);

            if (!(previousPosition == null))
            {
                var yDiff = currentPosition.Y - previousPosition.Value.Y;
                yDiff *= 1.5;
                var newCurrentValue = CoerceYOffsetPropertyCallback(null, YOffset - yDiff / cubeViewport3D.ActualHeight);
                YOffset = (double)newCurrentValue;
                Value = new Point3D(Value.X, YOffset, Value.Z);
            }

            previousPosition = currentPosition;
            Mouse.Capture(cubeViewport3D);
        }

        private void ContentGrid_MouseAction(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
                return;

            var mousePos = e.GetPosition(contentGrid);
            var limit = new Point(contentGrid.ActualWidth, contentGrid.ActualHeight);
            mousePos = mousePos.Coerce(new Point(0, 0), limit);

            Canvas.SetLeft(pointer, mousePos.X);
            Canvas.SetTop(pointer, mousePos.Y);

            var ratioX = mousePos.X / contentGrid.ActualWidth;
            var ratioY = mousePos.Y / contentGrid.ActualHeight;

            var offset = Maximum - Minimum;
            var scaledOffset = new Vector3D(offset.X * ratioX, YOffset, offset.Y * ratioY);

            Value = scaledOffset + Minimum;
            Mouse.Capture(contentGrid);
        }

        private void ContentGrid_MouseUp(object sender, MouseEventArgs e)
        {
            Mouse.Capture(null);
        }
    }
}
