public final class DistanceUtil {
    private static final double EARTH_RADIUS_KM = 6371.0;
    private static final double ROAD_FACTOR = 1.25;

    private DistanceUtil() {
    }

    public static double estimatedRoadDistanceKm(DeliveryPoint first, DeliveryPoint second) {
        double latitudeDistance = Math.toRadians(second.getLatitude() - first.getLatitude());
        double longitudeDistance = Math.toRadians(second.getLongitude() - first.getLongitude());

        double firstLatitude = Math.toRadians(first.getLatitude());
        double secondLatitude = Math.toRadians(second.getLatitude());

        double a = Math.sin(latitudeDistance / 2) * Math.sin(latitudeDistance / 2)
                + Math.cos(firstLatitude) * Math.cos(secondLatitude)
                * Math.sin(longitudeDistance / 2) * Math.sin(longitudeDistance / 2);
        double c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));

        return roundToOneDecimal(EARTH_RADIUS_KM * c * ROAD_FACTOR);
    }

    public static double roundToOneDecimal(double value) {
        return Math.round(value * 10.0) / 10.0;
    }
}
