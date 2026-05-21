import java.util.ArrayList;
import java.util.Comparator;
import java.util.HashSet;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Set;

public class RoutePlanner {
    private final Graph graph;
    private final int depotId;
    private final int vehicleCount;
    private ShortestPathTable shortestPaths;

    public RoutePlanner(Graph graph, int depotId, int vehicleCount) {
        if (vehicleCount < 1) {
            throw new IllegalArgumentException("Arac sayisi en az 1 olmalidir.");
        }
        this.graph = graph;
        this.depotId = depotId;
        this.vehicleCount = vehicleCount;
    }

    public List<Route> planRoutes(Set<Integer> orderPointIds) {
        validateOrderPoints(orderPointIds);
        Set<Integer> sourceNodes = new HashSet<>(orderPointIds);
        sourceNodes.add(depotId);
        shortestPaths = new ShortestPathTable(graph, sourceNodes);

        List<Integer> orders = new ArrayList<>(new LinkedHashSet<>(orderPointIds));
        orders.sort(Comparator.comparingDouble((Integer id) -> shortestPaths.distance(depotId, id)).reversed());

        List<Route> routes = new ArrayList<>();
        for (int i = 1; i <= vehicleCount; i++) {
            routes.add(new Route(i));
        }

        if (orders.isEmpty()) {
            return routes;
        }

        int activeVehicleCount = Math.min(vehicleCount, orders.size());
        int maxStopsPerVehicle = (int) Math.ceil(orders.size() / (double) activeVehicleCount);

        for (int i = 0; i < activeVehicleCount; i++) {
            routes.get(i).getStops().add(orders.get(i));
        }

        for (int i = activeVehicleCount; i < orders.size(); i++) {
            int orderPoint = orders.get(i);
            BestInsertion bestInsertion = findBestInsertion(routes, activeVehicleCount, orderPoint, maxStopsPerVehicle);
            routes.get(bestInsertion.routeIndex()).getStops().add(bestInsertion.insertIndex(), orderPoint);
        }

        for (Route route : routes) {
            improveWithTwoOpt(route);
        }

        return routes;
    }

    public ShortestPathTable getShortestPaths() {
        if (shortestPaths == null) {
            throw new IllegalStateException("Once planRoutes calistirilmalidir.");
        }
        return shortestPaths;
    }

    private BestInsertion findBestInsertion(List<Route> routes, int activeVehicleCount, int orderPoint, int maxStopsPerVehicle) {
        BestInsertion bestInsertion = null;

        for (int routeIndex = 0; routeIndex < activeVehicleCount; routeIndex++) {
            Route route = routes.get(routeIndex);
            if (route.getStops().size() >= maxStopsPerVehicle) {
                continue;
            }

            for (int insertIndex = 0; insertIndex <= route.getStops().size(); insertIndex++) {
                double increase = insertionCost(route, insertIndex, orderPoint);
                if (bestInsertion == null || increase < bestInsertion.costIncrease()) {
                    bestInsertion = new BestInsertion(routeIndex, insertIndex, increase);
                }
            }
        }

        if (bestInsertion == null) {
            return findBestInsertionWithoutLimit(routes, activeVehicleCount, orderPoint);
        }

        return bestInsertion;
    }

    private BestInsertion findBestInsertionWithoutLimit(List<Route> routes, int activeVehicleCount, int orderPoint) {
        BestInsertion bestInsertion = null;

        for (int routeIndex = 0; routeIndex < activeVehicleCount; routeIndex++) {
            Route route = routes.get(routeIndex);
            for (int insertIndex = 0; insertIndex <= route.getStops().size(); insertIndex++) {
                double increase = insertionCost(route, insertIndex, orderPoint);
                if (bestInsertion == null || increase < bestInsertion.costIncrease()) {
                    bestInsertion = new BestInsertion(routeIndex, insertIndex, increase);
                }
            }
        }

        return bestInsertion;
    }

    private double insertionCost(Route route, int insertIndex, int orderPoint) {
        int previousNode = insertIndex == 0 ? depotId : route.getStops().get(insertIndex - 1);
        int nextNode = insertIndex == route.getStops().size() ? depotId : route.getStops().get(insertIndex);

        return shortestPaths.distance(previousNode, orderPoint)
                + shortestPaths.distance(orderPoint, nextNode)
                - shortestPaths.distance(previousNode, nextNode);
    }

    private void improveWithTwoOpt(Route route) {
        if (route.getStops().size() < 3) {
            return;
        }

        boolean improved = true;
        while (improved) {
            improved = false;
            double currentDistance = route.totalDistanceKm(depotId, shortestPaths);

            for (int start = 0; start < route.getStops().size() - 1; start++) {
                for (int end = start + 1; end < route.getStops().size(); end++) {
                    reverse(route.getStops(), start, end);
                    double candidateDistance = route.totalDistanceKm(depotId, shortestPaths);

                    if (candidateDistance + 0.001 < currentDistance) {
                        currentDistance = candidateDistance;
                        improved = true;
                    } else {
                        reverse(route.getStops(), start, end);
                    }
                }
            }
        }
    }

    private void reverse(List<Integer> values, int start, int end) {
        while (start < end) {
            int temp = values.get(start);
            values.set(start, values.get(end));
            values.set(end, temp);
            start++;
            end--;
        }
    }

    private void validateOrderPoints(Set<Integer> orderPointIds) {
        for (int id : orderPointIds) {
            if (id == depotId) {
                throw new IllegalArgumentException("Depo siparis noktasi olarak secilemez.");
            }
            if (!graph.hasPoint(id)) {
                throw new IllegalArgumentException(id + " numarali siparis noktasi graf icinde bulunamadi.");
            }
        }
    }

    private static class BestInsertion {
        private final int routeIndex;
        private final int insertIndex;
        private final double costIncrease;

        private BestInsertion(int routeIndex, int insertIndex, double costIncrease) {
            this.routeIndex = routeIndex;
            this.insertIndex = insertIndex;
            this.costIncrease = costIncrease;
        }

        private int routeIndex() {
            return routeIndex;
        }

        private int insertIndex() {
            return insertIndex;
        }

        private double costIncrease() {
            return costIncrease;
        }
    }
}
