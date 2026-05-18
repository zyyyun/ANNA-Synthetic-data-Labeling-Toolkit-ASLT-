using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ASLTv1.Forms
{
    /// <summary>
    /// PictureBox subclass that uses Bilinear interpolation instead of the default
    /// HighQualityBicubic for image scaling.
    ///
    /// 260512-perf (Phase 1 of Option D+B-1):
    ///   1080p 영상을 ~1500x848 PictureBox 에 표시 시 GDI+ 의 default
    ///   HighQualityBicubic scaling 이 paintLatency 9-12ms 의 주요 비용.
    ///   Bilinear (filtered) 로 전환하여 ~3-5ms 로 단축. 1080p downsample 에서
    ///   시각 품질 차이는 미미하며 영상 재생 컨텍스트에서 충분.
    ///
    /// SizeMode, Image lifecycle, BBox drawing path 등 다른 동작은 모두 무변경.
    /// </summary>
    public class FastPictureBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.InterpolationMode = InterpolationMode.Bilinear;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
            base.OnPaint(e);
        }
    }
}
