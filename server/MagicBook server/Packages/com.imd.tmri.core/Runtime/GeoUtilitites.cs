using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TMRI.Core
{
    public class GeoCoordinateConverter
    {
        // WGS-84 geodetic constants
        const double a = 6378137.0;         // WGS-84 Earth semimajor axis (m)

        const double b = 6356752.314245;     // Derived Earth semiminor axis (m)
        const double f = (a - b) / a;           // Ellipsoid Flatness
        const double f_inv = 1.0 / f;       // Inverse flattening

        //const double f_inv = 298.257223563; // WGS-84 Flattening Factor of the Earth 
        //const double b = a - a / f_inv;
        //const double f = 1.0 / f_inv;

        const double a_sq = a * a;
        const double b_sq = b * b;
        const double e_sq = f * (2 - f);    // Square of Eccentricity

        // Earth's radius in meters (mean radius)
        private const double EarthRadius = 6378137.0;

        // Reference point in geodetic coordinates
        public static double referenceLatitude;
        public static double referenceLongitude;
        public static double referenceAltitude;
        public static double referenceElevation;

        public static Vector3 Unity4ToGeo(Vector3 unityPosition)
        {
            var altitude = unityPosition.y - referenceAltitude;
            //TODO x and z components
            return new Vector3(unityPosition.x, (float)altitude, unityPosition.z);
        }

        public static Vector3 GeoToUnity(double latitude, double longitude, double altitude)
        {
            return GeoToUnity(latitude, longitude, altitude, referenceLatitude, referenceLongitude, referenceAltitude, 1f);
        }

        /// <summary>
        /// Converts geodetic coordinates (latitude, longitude, altitude) to Unity coordinates
        /// relative to a reference point, projected onto the XZ-plane.
        /// </summary>
        public static Vector3 GeoToUnity(double latitude, double longitude, double altitude, double refLat, double refLng, double refAlt, float scale)
        {
            // Convert latitude and longitude from degrees to radians
            double refLatRad = refLat * Mathf.Deg2Rad;
            double refLonRad = refLng * Mathf.Deg2Rad;
            double latRad = latitude * Mathf.Deg2Rad;
            double lonRad = longitude * Mathf.Deg2Rad;

            // Compute the differences in latitude and longitude
            double deltaLat = latRad - refLatRad;
            double deltaLon = lonRad - refLonRad;

            // Approximate distances in meters for small angles
            double eastOffset = EarthRadius * deltaLon * Mathf.Cos((float)refLatRad); // East (X-axis)
            double northOffset = EarthRadius * deltaLat;
            //double heightOffset = -(altitude - refAlt);// North (Z-axis)
            double heightOffset = altitude + refAlt;// - (EarthRadius * (1f - Mathf.Cos((float)-deltaLat)));

            //if (deltaLat < 0f)
            //{
            //    heightOffset = altitude + refAlt - (EarthRadius * (1f - Mathf.Cos((float)deltaLat)));
            //}

            // Altitude is directly the Y-axis
            return new Vector3((float)eastOffset, (float)heightOffset, (float)northOffset) * scale;
        }

        public static Vector3 GeoToUnity2(double latitude, double longitude, double altitude, double refLat, double refLng, double refAlt, float scale)
        {
            GeodeticToEcef(latitude, longitude, altitude, out double ecefX, out double ecefY, out double ecefZ);
            EcefToEnu(ecefX, ecefY, ecefZ, refLat, refLng, refAlt, out double E, out double N, out double U);

            return new Vector3((float)E, (float)U, (float)N);
        }

        // Converts WGS-84 Geodetic point (lat, lon, h) to the 
        // Earth-Centered Earth-Fixed (ECEF) coordinates (x, y, z).
        public static void GeodeticToEcef(double lat, double lon, double h,
                                            out double x, out double y, out double z)
        {
            // Convert to radians in notation consistent with the paper:
            var lambda = DegreesToRadians(lat);
            var phi = DegreesToRadians(lon);
            var s = Math.Sin(lambda);
            var N = a / Math.Sqrt(1 - e_sq * s * s);

            var sin_lambda = Math.Sin(lambda);
            var cos_lambda = Math.Cos(lambda);
            var cos_phi = Math.Cos(phi);
            var sin_phi = Math.Sin(phi);

            x = (h + N) * cos_lambda * cos_phi;
            y = (h + N) * cos_lambda * sin_phi;
            z = (h + (1 - e_sq) * N) * sin_lambda;
        }

        // Converts the Earth-Centered Earth-Fixed (ECEF) coordinates (x, y, z) to 
        // East-North-Up coordinates in a Local Tangent Plane that is centered at the 
        // (WGS-84) Geodetic point (lat0, lon0, h0).
        public static void EcefToEnu(double x, double y, double z,
                                        double lat0, double lon0, double h0,
                                        out double xEast, out double yNorth, out double zUp)
        {
            // Convert to radians in notation consistent with the paper:
            var lambda = DegreesToRadians(lat0);
            var phi = DegreesToRadians(lon0);
            var s = Math.Sin(lambda);
            var N = a / Math.Sqrt(1 - e_sq * s * s);

            var sin_lambda = Math.Sin(lambda);
            var cos_lambda = Math.Cos(lambda);
            var cos_phi = Math.Cos(phi);
            var sin_phi = Math.Sin(phi);

            double x0 = (h0 + N) * cos_lambda * cos_phi;
            double y0 = (h0 + N) * cos_lambda * sin_phi;
            double z0 = (h0 + (1 - e_sq) * N) * sin_lambda;

            double xd, yd, zd;
            xd = x - x0;
            yd = y - y0;
            zd = z - z0;

            // This is the matrix multiplication
            xEast = -sin_phi * xd + cos_phi * yd;
            yNorth = -cos_phi * sin_lambda * xd - sin_lambda * sin_phi * yd + cos_lambda * zd;
            zUp = cos_lambda * cos_phi * xd + cos_lambda * sin_phi * yd + sin_lambda * zd;
        }

        static bool AreClose(double x0, double x1)
        {
            var d = x1 - x0;
            return (d * d) < 0.1;
        }


        static double DegreesToRadians(double degrees)
        {
            return Math.PI / 180.0 * degrees;
        }

        static double RadiansToDegrees(double radians)
        {
            return 180.0 / Math.PI * radians;
        }

    }

    // Struct for double-precision vector math
    public struct Vector3d
    {
        public double x, y, z;
        public Vector3d(double x, double y, double z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3d operator -(Vector3d a, Vector3d b) => new Vector3d(a.x - b.x, a.y - b.y, a.z - b.z);
    }

}