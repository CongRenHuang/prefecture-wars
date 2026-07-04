using UnityEngine;

namespace Game.Presentation.Map
{
    /// <summary>
    /// Equirectangular projection centered on Japan. Longitude is compressed by
    /// cos(latitude) so east-west spacing roughly matches real distance at the
    /// center latitude (~37°N). Output is in degrees of "corrected" lat/lon; the
    /// caller multiplies by a world scale. North is +Y (up), east is +X (right).
    /// </summary>
    public static class GeoProjection
    {
        public const float JapanLatCenter = 37f;
        public const float JapanLonCenter = 135f;

        public static Vector2 ToXY(float lat, float lon,
            float latCenter = JapanLatCenter, float lonCenter = JapanLonCenter)
        {
            float x = (lon - lonCenter) * Mathf.Cos(latCenter * Mathf.Deg2Rad);
            float y = lat - latCenter;
            return new Vector2(x, y);
        }
    }
}
