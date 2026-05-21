public class Edge {
    private final int from;
    private final int to;
    private final double distanceKm;

    public Edge(int from, int to, double distanceKm) {
        this.from = from;
        this.to = to;
        this.distanceKm = distanceKm;
    }

    public int getFrom() {
        return from;
    }

    public int getTo() {
        return to;
    }

    public double getDistanceKm() {
        return distanceKm;
    }
}
