# Changelog

All notable changes to FlagsRally are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.17] - Unreleased

### Changed
- **Development:** Every pull request now has to update this changelog. A hook blocks opening one that does not.

### Fixed
- **Custom board tiles leaving empty space on some devices.** A visited tile's image took whatever height the device measured for the downloaded picture (it depends on screen width and density), so visited and unvisited tiles in a row could differ in height and leave a gap below, for example on 全国神宮二十五社巡り. The image height now comes from the column width and the board's image shape. Devices that never showed the gap look the same as before.
- **The map moving when a card appeared.** Showing the check-in spot guide or the pin details shrank the map and shifted what was on screen. The map now keeps its size and the cards float over its lower edge.
- **Zoom buttons hidden under the cards.** The map's zoom buttons are now the app's own + / − buttons at the bottom right. They move up to sit just above whichever card is showing, and back down when it closes.
- **Google's directions and open-in-Maps buttons hidden under the cards.** These are now on the cards themselves. The check-in spot guide has directions and open-in-Maps icons, and the pin details card has an open-in-Maps icon beside its Directions button.
- **The map jumping to a far-away place after returning to the app.** Starting the app while it was already open (from another app, a notification or a link) created a second screen whose map opened at Google's default position. The app now reuses its existing screen. If Android rebuilds the map anyway, the map returns to where it was left.
