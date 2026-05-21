import java.util.ArrayList;
import java.util.Arrays;
import java.util.LinkedHashSet;
import java.util.List;
import java.util.Scanner;
import java.util.Set;

public class Main {
    private static final int DEPOT_ID = 0;
    private static final int DEFAULT_VEHICLE_COUNT = 4;

    public static void main(String[] args) {
        Graph graph = SampleData.createIspartaGraph();
        Scanner scanner = new Scanner(System.in);

        printHeader();
        printDeliveryPoints(graph);

        Set<Integer> orderPointIds = readOrderPoints(scanner);
        int vehicleCount = readVehicleCount(scanner);

        RoutePlanner planner = new RoutePlanner(graph, DEPOT_ID, vehicleCount);
        List<Route> routes = planner.planRoutes(orderPointIds);

        printRoutes(graph, DEPOT_ID, routes, planner.getShortestPaths());
    }

    private static void printHeader() {
        System.out.println("==============================================");
        System.out.println(" Isparta Gul Kremi Teslimat Rota Planlayici");
        System.out.println("==============================================");
        System.out.println("Gunluk siparis gelen noktalar secilir, graf uzerinde en kisa yollar");
        System.out.println("Dijkstra ile hesaplanir ve arac rotalari olusturulur.");
        System.out.println();
    }

    private static void printDeliveryPoints(Graph graph) {
        System.out.println("Teslimat noktalari:");
        for (DeliveryPoint point : graph.getPoints()) {
            if (point.getId() == DEPOT_ID) {
                System.out.printf("%2d) %-32s (baslangic ve bitis)%n", point.getId(), point.getName());
            } else {
                System.out.printf("%2d) %s%n", point.getId(), point.getName());
            }
        }
        System.out.println();
    }

    private static Set<Integer> readOrderPoints(Scanner scanner) {
        System.out.println("Siparis gelen nokta numaralarini virgulle giriniz.");
        System.out.println("Ornek: 1,3,5,8,12,16,20");
        System.out.print("Bos birakilirsa ornek gunluk siparis kullanilir: ");

        String input = scanner.nextLine().trim();
        if (input.isEmpty()) {
            return new LinkedHashSet<>(Arrays.asList(1, 3, 5, 7, 10, 12, 14, 16, 18, 20));
        }

        return parseOrderPointIds(input);
    }

    private static Set<Integer> parseOrderPointIds(String input) {
        Set<Integer> pointIds = new LinkedHashSet<>();
        String[] parts = input.split(",");

        for (String part : parts) {
            String cleaned = part.trim();
            if (cleaned.isEmpty()) {
                continue;
            }
            pointIds.add(Integer.parseInt(cleaned));
        }

        if (pointIds.isEmpty()) {
            throw new IllegalArgumentException("En az bir siparis noktasi girilmelidir.");
        }

        return pointIds;
    }

    private static int readVehicleCount(Scanner scanner) {
        System.out.print("Arac sayisi (bos birakilirsa 4): ");
        String input = scanner.nextLine().trim();

        if (input.isEmpty()) {
            return DEFAULT_VEHICLE_COUNT;
        }

        return Integer.parseInt(input);
    }

    private static void printRoutes(Graph graph, int depotId, List<Route> routes, ShortestPathTable shortestPaths) {
        double allRoutesTotal = 0.0;

        System.out.println();
        System.out.println("Olusturulan rotalar:");

        for (Route route : routes) {
            System.out.println("----------------------------------------------");
            System.out.printf("Arac %d%n", route.getVehicleNumber());

            if (route.isEmpty()) {
                System.out.println("Bu araca siparis atanmadı.");
                continue;
            }

            double routeDistance = route.totalDistanceKm(depotId, shortestPaths);
            allRoutesTotal += routeDistance;

            List<Integer> routeWithDepot = new ArrayList<>();
            routeWithDepot.add(depotId);
            routeWithDepot.addAll(route.getStopsView());
            routeWithDepot.add(depotId);

            System.out.println("Durak sirasi: " + formatPointNames(graph, routeWithDepot));
            System.out.printf("Rota mesafesi: %.1f km%n", routeDistance);
            System.out.println("Graf uzerindeki en kisa yol parcalari:");

            for (int i = 0; i < routeWithDepot.size() - 1; i++) {
                int from = routeWithDepot.get(i);
                int to = routeWithDepot.get(i + 1);
                List<Integer> path = shortestPaths.path(from, to);
                System.out.printf("  %s -> %s: %.1f km | %s%n",
                        graph.getPoint(from).getName(),
                        graph.getPoint(to).getName(),
                        shortestPaths.distance(from, to),
                        graph.formatPath(path));
            }
        }

        System.out.println("----------------------------------------------");
        System.out.printf("Toplam teslimat mesafesi: %.1f km%n", DistanceUtil.roundToOneDecimal(allRoutesTotal));
    }

    private static String formatPointNames(Graph graph, List<Integer> pointIds) {
        List<String> names = new ArrayList<>();
        for (int id : pointIds) {
            names.add(graph.getPoint(id).getName());
        }
        return String.join(" -> ", names);
    }
}
