// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
#if !WINAPPSDK
using Windows.UI.Xaml.Hosting;
#endif

namespace Hi3Helper.CommunityToolkit.WinUI.Controls;

/// <summary>
/// The <see cref="ImageCropper"/> control allows user to crop image freely.
/// </summary>
public partial class ImageCropper
{
    /// <summary>
    /// Initializes image source transform.
    /// </summary>
    /// <param name="animate">Whether animation is enabled.</param>
    private void InitImageLayout(bool animate = false)
    {
        if (Source != null)
        {
            _restrictedCropRect = new Rect(0, 0, Source.PixelWidth, Source.PixelHeight);
            if (IsValidRect(_restrictedCropRect))
            {
                _currentCroppedRect = KeepAspectRatio ? GetUniformRect(_restrictedCropRect, ActualAspectRatio) : _restrictedCropRect;

                if (TryUpdateImageLayout(animate))
                {
                    UpdateSelectionThumbs(animate);
                    UpdateMaskArea(animate);
                }
            }
        }

        UpdateThumbsVisibility();
    }

    /// <summary>
    /// Update image source transform.
    /// </summary>
    /// <param name="animate">Whether animation is enabled.</param>
    private bool TryUpdateImageLayout(bool animate = false)
    {
        if (Source != null && IsValidRect(CanvasRect))
        {
            var uniformSelectedRect = GetUniformRect(CanvasRect, _currentCroppedRect.Width / _currentCroppedRect.Height);

            return TryUpdateImageLayoutWithViewport(uniformSelectedRect, _currentCroppedRect, animate);
        }

        return false;
    }

    /// <summary>
    /// Update image source transform.
    /// </summary>
    /// <param name="viewport">Viewport</param>
    /// <param name="viewportImageRect"> The real image area of viewport.</param>
    /// <param name="animate">Whether animation is enabled.</param>
    private bool TryUpdateImageLayoutWithViewport(Rect viewport, Rect viewportImageRect, bool animate = false)
    {
        if (!IsValidRect(viewport) || !IsValidRect(viewportImageRect))
        {
            return false;
        }

        var imageScale = viewport.Width / viewportImageRect.Width;
        _imageTransform.ScaleX = _imageTransform.ScaleY = imageScale;
        _imageTransform.TranslateX = viewport.X - (viewportImageRect.X * imageScale);
        _imageTransform.TranslateY = viewport.Y - (viewportImageRect.Y * imageScale);
        _inverseImageTransform.ScaleX = _inverseImageTransform.ScaleY = 1 / imageScale;
        _inverseImageTransform.TranslateX = -_imageTransform.TranslateX / imageScale;
        _inverseImageTransform.TranslateY = -_imageTransform.TranslateY / imageScale;
        _restrictedSelectRect = _imageTransform.TransformBounds(_restrictedCropRect);

        if (animate)
        {
            AnimateUIElementOffset(new Point(_imageTransform.TranslateX, _imageTransform.TranslateY), _animationDuration, _sourceImage!);
            AnimateUIElementScale(imageScale, _animationDuration, _sourceImage!);
        }
        else
        {
            var targetVisual = ElementCompositionPreview.GetElementVisual(_sourceImage);
            targetVisual.Offset = new Vector3((float)_imageTransform.TranslateX, (float)_imageTransform.TranslateY, 0);
            targetVisual.Scale = new Vector3((float)imageScale);
        }

        return true;
    }

    /// <summary>
    /// Update cropped area.
    /// </summary>
    /// <param name="position">The control point</param>
    /// <param name="diffPos">Position offset</param>
    private void UpdateCroppedRect(ThumbPosition position, Point diffPos)
    {
        if (diffPos == default(Point) || !IsValidRect(CanvasRect))
        {
            return;
        }

        double radian = 0d, diffPointRadian = 0d;
        if (KeepAspectRatio)
        {
            radian = Math.Atan(ActualAspectRatio);
            diffPointRadian = Math.Atan(diffPos.X / diffPos.Y);
        }

        var startPoint = new Point(_startX, _startY);
        var endPoint = new Point(_endX, _endY);
        var currentSelectedRect = startPoint.ToRect(endPoint);
        switch (position)
        {
            case ThumbPosition.Top:
                if (KeepAspectRatio)
                {
                    var originSizeChange = new Point(-diffPos.Y * ActualAspectRatio, -diffPos.Y);
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    startPoint.X += -safeChange.X / 2;
                    endPoint.X += safeChange.X / 2;
                    startPoint.Y += -safeChange.Y;
                }
                else
                {
                    startPoint.Y += diffPos.Y;
                }

                break;
            case ThumbPosition.Bottom:
                if (KeepAspectRatio)
                {
                    var originSizeChange = new Point(diffPos.Y * ActualAspectRatio, diffPos.Y);
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    startPoint.X += -safeChange.X / 2;
                    endPoint.X += safeChange.X / 2;
                    endPoint.Y += safeChange.Y;
                }
                else
                {
                    endPoint.Y += diffPos.Y;
                }

                break;
            case ThumbPosition.Left:
                if (KeepAspectRatio)
                {
                    var originSizeChange = new Point(-diffPos.X, -diffPos.X / ActualAspectRatio);
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    startPoint.Y += -safeChange.Y / 2;
                    endPoint.Y += safeChange.Y / 2;
                    startPoint.X += -safeChange.X;
                }
                else
                {
                    startPoint.X += diffPos.X;
                }

                break;
            case ThumbPosition.Right:
                if (KeepAspectRatio)
                {
                    var originSizeChange = new Point(diffPos.X, diffPos.X / ActualAspectRatio);
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    startPoint.Y += -safeChange.Y / 2;
                    endPoint.Y += safeChange.Y / 2;
                    endPoint.X += safeChange.X;
                }
                else
                {
                    endPoint.X += diffPos.X;
                }

                break;
            case ThumbPosition.UpperLeft:
                if (KeepAspectRatio)
                {
                    var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                    var originSizeChange = new Point(-effectiveLength * Math.Sin(radian), -effectiveLength * Math.Cos(radian));
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    diffPos.X = -safeChange.X;
                    diffPos.Y = -safeChange.Y;
                }

                startPoint.X += diffPos.X;
                startPoint.Y += diffPos.Y;
                break;
            case ThumbPosition.UpperRight:
                if (KeepAspectRatio)
                {
                    diffPointRadian = -diffPointRadian;
                    var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                    var originSizeChange = new Point(-effectiveLength * Math.Sin(radian), -effectiveLength * Math.Cos(radian));
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    diffPos.X = safeChange.X;
                    diffPos.Y = -safeChange.Y;
                }

                endPoint.X += diffPos.X;
                startPoint.Y += diffPos.Y;
                break;
            case ThumbPosition.LowerLeft:
                if (KeepAspectRatio)
                {
                    diffPointRadian = -diffPointRadian;
                    var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                    var originSizeChange = new Point(effectiveLength * Math.Sin(radian), effectiveLength * Math.Cos(radian));
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    diffPos.X = -safeChange.X;
                    diffPos.Y = safeChange.Y;
                }

                startPoint.X += diffPos.X;
                endPoint.Y += diffPos.Y;
                break;
            case ThumbPosition.LowerRight:
                if (KeepAspectRatio)
                {
                    var effectiveLength = diffPos.Y / Math.Cos(diffPointRadian) * Math.Cos(diffPointRadian - radian);
                    var originSizeChange = new Point(effectiveLength * Math.Sin(radian), effectiveLength * Math.Cos(radian));
                    var safeChange = GetSafeSizeChangeWhenKeepAspectRatio(_restrictedSelectRect, position, currentSelectedRect, originSizeChange, ActualAspectRatio);
                    diffPos.X = safeChange.X;
                    diffPos.Y = safeChange.Y;
                }

                endPoint.X += diffPos.X;
                endPoint.Y += diffPos.Y;
                break;
        }

        if (!IsSafeRect(startPoint, endPoint, MinSelectSize))
        {
            if (KeepAspectRatio)
            {
                if ((endPoint.Y - startPoint.Y) < (_endY - _startY) ||
                    (endPoint.X - startPoint.X) < (_endX - _startX))
                {
                    return;
                }
            }
            else
            {
                var safeRect = GetSafeRect(startPoint, endPoint, MinSelectSize, position);
                safeRect.Intersect(_restrictedSelectRect);
                startPoint = new Point(safeRect.X, safeRect.Y);
                endPoint = new Point(safeRect.X + safeRect.Width, safeRect.Y + safeRect.Height);
            }
        }

        var isEffectiveRegion = IsSafePoint(_restrictedSelectRect, startPoint) &&
                                IsSafePoint(_restrictedSelectRect, endPoint);
        var selectedRect = startPoint.ToRect(endPoint);
        if (!isEffectiveRegion)
        {
            if (!IsCornerThumb(position) && TryGetContainedRect(_restrictedSelectRect, ref selectedRect))
            {
                startPoint = new Point(selectedRect.Left, selectedRect.Top);
                endPoint = new Point(selectedRect.Right, selectedRect.Bottom);
            }
            else
            {
                return;
            }
        }

        selectedRect.Union(CanvasRect);
        if (selectedRect != CanvasRect)
        {
            var croppedRect = _inverseImageTransform.TransformBounds(startPoint.ToRect(endPoint));
            croppedRect.Intersect(_restrictedCropRect);
            _currentCroppedRect = croppedRect;
            var viewportRect = GetUniformRect(CanvasRect, selectedRect.Width / selectedRect.Height);
            var viewportImgRect = _inverseImageTransform.TransformBounds(selectedRect);

            if (TryUpdateImageLayoutWithViewport(viewportRect, viewportImgRect))
            {
                UpdateSelectionThumbs();
                UpdateMaskArea();
            }
        }
        else
        {
            UpdateSelectionThumbs(startPoint, endPoint);
            UpdateMaskArea();
        }
    }

    private void UpdateSelectionThumbs(bool animate = false)
    {
        var selectedRect = _imageTransform.TransformBounds(_currentCroppedRect);
        var startPoint = GetSafePoint(_restrictedSelectRect, new Point(selectedRect.X, selectedRect.Y));
        var endPoint = GetSafePoint(_restrictedSelectRect, new Point(selectedRect.X + selectedRect.Width, selectedRect.Y + selectedRect.Height));

        UpdateSelectionThumbs(startPoint, endPoint, animate);
    }

    #nullable enable
    /// <summary>
    /// Positions the thumbs for the selection rectangle.
    /// </summary>
    /// <param name="startPoint">The point on the upper left corner.</param>
    /// <param name="endPoint">The point on the lower right corner.</param>
    /// <param name="animate">Whether animation is enabled.</param>
    private void UpdateSelectionThumbs(Point startPoint, Point endPoint, bool animate = false)
    {
        _startX = startPoint.X;
        _startY = startPoint.Y;
        _endX = endPoint.X;
        _endY = endPoint.Y;
        var center = SelectionAreaCenter;

        Storyboard? storyboard = null;
        if (animate)
        {
            storyboard = new Storyboard();
        }

        if (_topThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(center.X, _animationDuration, _topThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(_startY, _animationDuration, _topThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _topThumb.X = center.X;
                _topThumb.Y = _startY;
            }
        }

        if (_bottomThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(center.X, _animationDuration, _bottomThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(_endY, _animationDuration, _bottomThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _bottomThumb.X = center.X;
                _bottomThumb.Y = _endY;
            }
        }

        if (_leftThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(_startX, _animationDuration, _leftThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(center.Y, _animationDuration, _leftThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _leftThumb.X = _startX;
                _leftThumb.Y = center.Y;
            }
        }

        if (_rightThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(_endX, _animationDuration, _rightThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(center.Y, _animationDuration, _rightThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _rightThumb.X = _endX;
                _rightThumb.Y = center.Y;
            }
        }

        if (_upperLeftThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(_startX, _animationDuration, _upperLeftThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(_startY, _animationDuration, _upperLeftThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _upperLeftThumb.X = _startX;
                _upperLeftThumb.Y = _startY;
            }
        }

        if (_upperRightThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(_endX, _animationDuration, _upperRightThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(_startY, _animationDuration, _upperRightThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _upperRightThumb.X = _endX;
                _upperRightThumb.Y = _startY;
            }
        }

        if (_lowerLeftThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(_startX, _animationDuration, _lowerLeftThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(_endY, _animationDuration, _lowerLeftThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _lowerLeftThumb.X = _startX;
                _lowerLeftThumb.Y = _endY;
            }
        }

        if (_lowerRigthThumb != null)
        {
            if (animate)
            {
                storyboard?.Children.Add(CreateDoubleAnimation(_endX, _animationDuration, _lowerRigthThumb, nameof(ImageCropperThumb.X), true));
                storyboard?.Children.Add(CreateDoubleAnimation(_endY, _animationDuration, _lowerRigthThumb, nameof(ImageCropperThumb.Y), true));
            }
            else
            {
                _lowerRigthThumb.X = _endX;
                _lowerRigthThumb.Y = _endY;
            }
        }

        if (animate)
        {
            storyboard?.Begin();
        }
    }
    #nullable restore

    /// <summary>
    /// Update crop shape.
    /// </summary>
    private void UpdateCropShape()
    {
        _maskAreaGeometryGroup.Children.Clear();
        _outerGeometry = new RectangleGeometry();
        switch (CropShape)
        {
            case CropShape.Rectangular:
                _innerGeometry = new RectangleGeometry();
                _overlayGeometry = new RectangleGeometry();
                break;
            case CropShape.Circular:
                _innerGeometry = new EllipseGeometry();
                _overlayGeometry = new EllipseGeometry();
                break;
        }

        _maskAreaGeometryGroup.Children.Add(_outerGeometry);
        _maskAreaGeometryGroup.Children.Add(_innerGeometry);
        if (_overlayAreaPath != null)
        {
            _overlayAreaPath.Data = _overlayGeometry;
        }
    }

    /// <summary>
    /// Update the mask layer.
    /// </summary>
    private void UpdateMaskArea(bool animate = false)
    {
        if (_layoutGrid == null || _maskAreaGeometryGroup.Children.Count < 2)
        {
            return;
        }

        _outerGeometry.Rect = new Rect(-_layoutGrid.Padding.Left, -_layoutGrid.Padding.Top, _layoutGrid.ActualWidth, _layoutGrid.ActualHeight);

        switch (CropShape)
        {
            case CropShape.Rectangular:
                var updateRectangleGeometry = (RectangleGeometry rectangleGeometry) =>
                {
                    var to = new Point(_startX, _startY).ToRect(new Point(_endX, _endY));
                    if (animate)
                    {
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(CreateRectangleAnimation(to, _animationDuration, rectangleGeometry, true));
                        storyboard.Begin();
                    }
                    else
                    {
                        rectangleGeometry.Rect = to;
                    }
                };
                if (_innerGeometry is RectangleGeometry innerRectangleGeometry)
                {
                    updateRectangleGeometry(innerRectangleGeometry);
                }
                if (_overlayGeometry is RectangleGeometry overlayRectangleGeometry)
                {
                    updateRectangleGeometry(overlayRectangleGeometry);
                }

                break;
            case CropShape.Circular:
                var updateEllipseGeometry = (EllipseGeometry ellipseGeometry) =>
                {
                    var center = new Point(((_endX - _startX) / 2) + _startX, ((_endY - _startY) / 2) + _startY);
                    var radiusX = (_endX - _startX) / 2;
                    var radiusY = (_endY - _startY) / 2;
                    if (animate)
                    {
                        var storyboard = new Storyboard();
                        storyboard.Children.Add(CreatePointAnimation(center, _animationDuration, ellipseGeometry, nameof(EllipseGeometry.Center), true));
                        storyboard.Children.Add(CreateDoubleAnimation(radiusX, _animationDuration, ellipseGeometry, nameof(EllipseGeometry.RadiusX), true));
                        storyboard.Children.Add(CreateDoubleAnimation(radiusY, _animationDuration, ellipseGeometry, nameof(EllipseGeometry.RadiusY), true));
                        storyboard.Begin();
                    }
                    else
                    {
                        ellipseGeometry.Center = center;
                        ellipseGeometry.RadiusX = radiusX;
                        ellipseGeometry.RadiusY = radiusY;
                    }
                };
                if (_innerGeometry is EllipseGeometry innerEllipseGeometry)
                {
                    updateEllipseGeometry(innerEllipseGeometry);
                }
                if (_overlayGeometry is EllipseGeometry overlayEllipseGeometry)
                {
                    updateEllipseGeometry(overlayEllipseGeometry);
                }

                break;
        }

        _layoutGrid.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, _layoutGrid.ActualWidth, _layoutGrid.ActualHeight)
        };
    }

    /// <summary>
    /// Update image aspect ratio.
    /// </summary>
    private bool TryUpdateAspectRatio()
    {
        if (KeepAspectRatio is false || Source is null || IsValidRect(_restrictedSelectRect) is false)
        {
            return false;
        }

        var center = SelectionAreaCenter;
        var restrictedMinLength = MinCroppedPixelLength * _imageTransform.ScaleX;
        var maxSelectedLength = Math.Max(_endX - _startX, _endY - _startY);
        var viewRect = new Rect(center.X - (maxSelectedLength / 2), center.Y - (maxSelectedLength / 2), maxSelectedLength, maxSelectedLength);

        var uniformSelectedRect = GetUniformRect(viewRect, ActualAspectRatio);
        if (uniformSelectedRect.Width > _restrictedSelectRect.Width || uniformSelectedRect.Height > _restrictedSelectRect.Height)
        {
            uniformSelectedRect = GetUniformRect(_restrictedSelectRect, ActualAspectRatio);
        }

        // If selection area is smaller than allowed.
        if (uniformSelectedRect.Width < restrictedMinLength || uniformSelectedRect.Height < restrictedMinLength)
        {
            // Scale selection area to fit.
            var scale = restrictedMinLength / Math.Min(uniformSelectedRect.Width, uniformSelectedRect.Height);
            uniformSelectedRect.Width *= scale;
            uniformSelectedRect.Height *= scale;

            // If selection area is larger than allowed.
            if (uniformSelectedRect.Width > _restrictedSelectRect.Width || uniformSelectedRect.Height > _restrictedSelectRect.Height)
            {
                // Sentinal value. Equivelant to setting KeepAspectRatio to false. Causes AspectRatio to be recalculated.
                AspectRatio = -1;
                return false;
            }
        }

        // Fix positioning
        if (_restrictedSelectRect.X > uniformSelectedRect.X)
        {
            uniformSelectedRect.X += _restrictedSelectRect.X - uniformSelectedRect.X;
        }

        if (_restrictedSelectRect.Y > uniformSelectedRect.Y)
        {
            uniformSelectedRect.Y += _restrictedSelectRect.Y - uniformSelectedRect.Y;
        }

        // Fix size
        if ((_restrictedSelectRect.X + _restrictedSelectRect.Width) < (uniformSelectedRect.X + uniformSelectedRect.Width))
        {
            uniformSelectedRect.X += (_restrictedSelectRect.X + _restrictedSelectRect.Width) - (uniformSelectedRect.X + uniformSelectedRect.Width);
        }

        if ((_restrictedSelectRect.Y + _restrictedSelectRect.Height) < (uniformSelectedRect.Y + uniformSelectedRect.Height))
        {
            uniformSelectedRect.Y += (_restrictedSelectRect.Y + _restrictedSelectRect.Height) - (uniformSelectedRect.Y + uniformSelectedRect.Height);
        }

        // Apply transformation
        var croppedRect = _inverseImageTransform.TransformBounds(uniformSelectedRect);
        croppedRect.Intersect(_restrictedCropRect);
        _currentCroppedRect = croppedRect;

        return true;
    }

    /// <summary>
    /// Update the visibility of the thumbs.
    /// </summary>
    private void UpdateThumbsVisibility()
    {
        var cornerThumbsVisibility = Visibility.Visible;
        var otherThumbsVisibility = Visibility.Visible;
        switch (ThumbPlacement)
        {
            case ThumbPlacement.All:
                break;
            case ThumbPlacement.Corners:
                otherThumbsVisibility = Visibility.Collapsed;
                break;
        }

        switch (CropShape)
        {
            case CropShape.Rectangular:
                break;
            case CropShape.Circular:
                cornerThumbsVisibility = Visibility.Collapsed;
                otherThumbsVisibility = Visibility.Visible;
                break;
        }

        if (Source == null)
        {
            cornerThumbsVisibility = otherThumbsVisibility = Visibility.Collapsed;
        }

        if (_topThumb != null)
        {
            _topThumb.Visibility = otherThumbsVisibility;
        }

        if (_bottomThumb != null)
        {
            _bottomThumb.Visibility = otherThumbsVisibility;
        }

        if (_leftThumb != null)
        {
            _leftThumb.Visibility = otherThumbsVisibility;
        }

        if (_rightThumb != null)
        {
            _rightThumb.Visibility = otherThumbsVisibility;
        }

        if (_upperLeftThumb != null)
        {
            _upperLeftThumb.Visibility = cornerThumbsVisibility;
        }

        if (_upperRightThumb != null)
        {
            _upperRightThumb.Visibility = cornerThumbsVisibility;
        }

        if (_lowerLeftThumb != null)
        {
            _lowerLeftThumb.Visibility = cornerThumbsVisibility;
        }

        if (_lowerRigthThumb != null)
        {
            _lowerRigthThumb.Visibility = cornerThumbsVisibility;
        }
    }

    /// <summary>
    /// Gets a value that indicates the center of the visible selection rectangle.
    /// </summary>
    private Point SelectionAreaCenter => new Point(((_endX - _startX) / 2) + _startX, ((_endY - _startY) / 2) + _startY);
}
