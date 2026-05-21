import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

public class Route {
    private final int vehicleNumber;
    private final List<Integer> stops = new ArrayList<>();

    public Route(int vehicleNumber) {
        this.vehicleNumber = vehicleNumber;
    }

    public int getVehicleNumber() {
        return vehicleNumber;
    }

    public List<Integer> getStops() {
        return stops;
    }

    public List<Integer> getStopsView() {
        return Collections.unmodifiableList(stops);
    }

    public boolean isEmpty() {
        return stops.isEmpty();
    }

    public double totalDistanceKm(int depotId, ShortestPathTable shortestPaths) {
        if (stops.isEmpty()) {
            return 0.0;
        }

        double total = 0.0;
        int previous = depotId;

        for (int stop : stops) {
            total += shortestPaths.distance(previous, stop);
            previous = stop;
        }

        total += shortestPaths.distance(previous, depotId);
        return DistanceUtil.roundToOneDecimal(total);
    }
}
