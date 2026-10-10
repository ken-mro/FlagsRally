# Changelog

All notable changes to FlagsRally are recorded here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [1.1.18] - Unreleased

### Changed
- **Get Location works like a long press.** It puts the check-in spot at where you are and shows its card, just as a long press does at the place you pressed. Recording is then always "Check in here" on that card, instead of Get Location going straight to the confirmation.
- **The pin card's Directions button is gone.** It did the same as the directions button on the map.
- **Directions and open-in-Maps are back on the map.** They are round buttons above the zoom buttons, shown while a card shows a place, like Google's own toolbar, instead of small icons squeezed into the cards' title rows. The cards have room for their titles again on narrow phones.
- **Development:** Every pull request now has to update this changelog. A hook blocks opening one that does not.
- **Development:** A test now fails when any text other than a board, country or region name is set to be cut off, or when the app's own text is used as a placeholder. Layout changes are checked on both Android and iOS, with large text in English and Japanese.

### Fixed
- **Map card text cut off after the directions and open-in-Maps icons were added.** The check-in spot guide's title ("Check in at this pin" / このピンの場所で記録) and the pin details card's title and subtitle now wrap instead of ending in "…". The pin card's buttons move to a second line when they do not fit, instead of squeezing Check In down to "チ" in Japanese with large text.
- **Long board names cut off in Add Board and Manage Boards.** Add Board shows the whole name, and Manage Boards shows up to two lines.
- **"All Countries…" cut off on the Passport tab on iPhone.** The country filter now fits at the normal text size and wraps onto a second line with large text.
- **Stamp and tile dates cut off with large text.** The arrival date on passport stamps and board tiles ("09 Oct 20…") wraps instead.
- **The Google Maps API key hint cut off in Settings.** The hint ("使用する場合は入力してくだ…") is now shown in full above the box instead of inside it.
- **The selected tab's name cut off on Android** ("コレクシ…" with Japanese and large text). It is now the same size as the other tabs.

## [1.1.17] - 2026-10-10

### Fixed
- **Custom board tiles leaving empty space on some devices.** A visited tile's image took whatever height the device measured for the downloaded picture (it depends on screen width and density), so visited and unvisited tiles in a row could differ in height and leave a gap below, for example on 全国神宮二十五社巡り. The image height now comes from the column width and the board's image shape. Devices that never showed the gap look the same as before.
- **The map moving when a card appeared.** Showing the check-in spot guide or the pin details shrank the map and shifted what was on screen. The map now keeps its size and the cards float over its lower edge.
- **Zoom buttons hidden under the cards.** The map's zoom buttons are now the app's own + / − buttons at the bottom right. They move up to sit just above whichever card is showing, and back down when it closes.
- **Google's directions and open-in-Maps buttons hidden under the cards.** These are now on the cards themselves. The check-in spot guide has directions and open-in-Maps icons, and the pin details card has an open-in-Maps icon beside its Directions button.
- **The map jumping to a far-away place after returning to the app.** Starting the app while it was already open (from another app, a notification or a link) created a second screen whose map opened at Google's default position. The app now reuses its existing screen. If Android rebuilds the map anyway, the map returns to where it was left.
