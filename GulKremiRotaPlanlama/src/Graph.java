import java.util.ArrayList;
import java.util.Collection;
import java.util.Collections;
import java.util.Comparator;
import java.util.HashMap;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.PriorityQueue;

public class Graph {
    private final Map<Integer, DeliveryPoint> points = new LinkedHashMap<>();
    private final Map<Integer, List<Edge>> adjacencyList = new LinkedHashMap<>();

    public void addPoint(DeliveryPoint point) {
        points.put(point.getId(), point);
        adjacencyList.putIfAbsent(point.getId(), new ArrayList<>());
    }

    public void addUndirectedEdge(int firstNode, int secondNode, double distanceKm) {
        ensurePointExists(firstNode);
        ensurePointExists(secondNode);
        adjacencyList.get(firstNode).add(new Edge(firstNode, secondNode, distanceKm));
        adjacencyList.get(secondNode).add(new Edge(secondNode, firstNode, distanceKm));
    }

    public DeliveryPoint getPoint(int id) {
        ensurePointExists(id);
        return points.get(id);
    }

    public boolean hasPoint(int id) {
        return points.containsKey(id);
    }

    public Collection<DeliveryPoint> getPoints() {
        return Collections.unmodifiableCollection(points.values());
    }

    public List<Edge> getNeighbors(int nodeId) {
        ensurePointExists(nodeId);
        return Collections.unmodifiableList(adjacencyList.get(nodeId));
    }

    public DijkstraResult shortestPathsFrom(int startNode) {
        ensurePointExists(startNode);

        Map<Integer, Double> distances = new HashMap<>();
        Map<Integer, Integer> previousNodes = new HashMap<>();
        PriorityQueue<NodeDistance> queue = new PriorityQueue<>(Comparator.comparingDouble(NodeDistance::distance));

        for (Integer id : points.keySet()) {
            distances.put(id, Double.POSITIVE_INFINITY);
        }

        distances.put(startNode, 0.0);
        queue.add(new NodeDistance(startNode, 0.0));

        while (!queue.isEmpty()) {
            NodeDistance current = queue.poll();
            if (current.distance() > distances.get(current.nodeId())) {
                continue;
            }

            for (Edge edge : adjacencyList.get(current.nodeId())) {
                double candidateDistance = current.distance() + edge.getDistanceKm();
                if (candidateDistance < distances.get(edge.getTo())) {
                    distances.put(edge.getTo(), candidateDistance);
                    previousNodes.put(edge.getTo(), current.nodeId());
                    queue.add(new NodeDistance(edge.getTo(), candidateDistance));
                }
            }
        }

        return new DijkstraResult(startNode, distances, previousNodes);
    }

    public String formatPath(List<Integer> path) {
        List<String> names = new ArrayList<>();
        for (int id : path) {
            names.add(getPoint(id).getName());
        }
        return String.join(" -> ", names);
    }

    private void ensurePointExists(int id) {
        if (!points.containsKey(id)) {
            throw new IllegalArgumentException("Graf icinde " + id + " numarali nokta bulunamadi.");
        }
    }

    private static class NodeDistance {
        private final int nodeId;
        private final double distance;

        private NodeDistance(int nodeId, double distance) {
            this.nodeId = nodeId;
            this.distance = distance;
        }

        private int nodeId() {
            return nodeId;
        }

        private double distance() {
            return distance;
        }
    }
}
