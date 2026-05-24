# KampusRota Design Rules

## Product
KampusRota is a mobile ride-sharing app for university students. It should feel trustworthy, efficient, and modern, similar to BlaBlaCar but focused on campus routes.

## Visual Style
- Use a clean professional mobile app style.
- Prefer calm dark navy, graphite, white, light gray, and one energetic accent color.
- Avoid childish visuals, heavy gradients, and clutter.
- Use clear hierarchy: search areas first, cards second, actions always obvious.
- Cards should have modest rounded corners, subtle borders, and enough spacing.

## Components
- Primary button: dark background, white text, bold, high contrast.
- Secondary button: white or light gray background, dark text.
- Destructive action: red text or red button only for delete/logout.
- Inputs: filled light background, visible label or placeholder, large touch target.
- Ride cards: route, driver, date, price, seats, badges, and detail action.
- Driver blocks: name, rating stars, trust label, contact information.
- Map screens: full screen map, close button top-right.

## Screens To Preserve
- Login
- Register
- Main ride search and listing
- Map picker
- Add ride
- Ride detail / contact
- Profile
- Change password

## Accessibility
- Keep text readable on mobile.
- Do not place white text on low contrast backgrounds.
- Buttons must be easy to tap.
- Long Turkish location names must fit without overlapping.

## Implementation Target
The design will be implemented in .NET MAUI XAML. Keep layouts realistic for MAUI controls such as Grid, VerticalStackLayout, CollectionView, Border, Button, Entry, Picker, DatePicker, TimePicker, CheckBox, WebView, and Image.
