using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Watcher_PC
{
    public partial class SelectionWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging;

        public System.Drawing.Rectangle SelectedRegion { get; private set; }
        public bool IsConfirmed { get; private set; }

        public SelectionWindow()
        {
            InitializeComponent();

            // Add global key handler for Escape
            this.KeyDown += SelectionWindow_KeyDown;
        }

        private void SelectionWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                IsConfirmed = false;
                Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _startPoint = e.GetPosition(SelectionCanvas);
                _isDragging = true;

                // Reset border
                Canvas.SetLeft(SelectionBorder, _startPoint.X);
                Canvas.SetTop(SelectionBorder, _startPoint.Y);
                SelectionBorder.Width = 0;
                SelectionBorder.Height = 0;
                SelectionBorder.Visibility = Visibility.Visible;

                // Capture mouse to ensure we get events even if user drags outside
                SelectionCanvas.CaptureMouse();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                // Cancel
                IsConfirmed = false;
                Close();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var curPoint = e.GetPosition(SelectionCanvas);

                double x = Math.Min(curPoint.X, _startPoint.X);
                double y = Math.Min(curPoint.Y, _startPoint.Y);
                double w = Math.Abs(curPoint.X - _startPoint.X);
                double h = Math.Abs(curPoint.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionBorder, x);
                Canvas.SetTop(SelectionBorder, y);
                SelectionBorder.Width = w;
                SelectionBorder.Height = h;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                SelectionCanvas.ReleaseMouseCapture();

                var curPoint = e.GetPosition(SelectionCanvas);

                int x = (int)Math.Min(curPoint.X, _startPoint.X);
                int y = (int)Math.Min(curPoint.Y, _startPoint.Y);
                int w = (int)Math.Abs(curPoint.X - _startPoint.X);
                int h = (int)Math.Abs(curPoint.Y - _startPoint.Y);

                // Small threshold to prevent accidental clicks
                if (w > 10 && h > 10)
                {
                    SelectedRegion = new System.Drawing.Rectangle(x, y, w, h);
                    IsConfirmed = true;
                    this.DialogResult = true;
                    Close();
                }
                else
                {
                    // If selection was too small (accidental click), reset UI
                    SelectionBorder.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
