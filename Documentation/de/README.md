# GrowingCity-Prototyp

[English](../en/README.md) · [한국어](../ko/README.md) · [日本語](../ja/README.md) · [简体中文](../zh-Hans/README.md) · [繁體中文](../zh-Hant/README.md) · [Deutsch](../de/README.md) · [Français](../fr/README.md) · [Español](../es/README.md) · [Português](../pt/README.md)

Bombardiere eine automatisch wachsende Stadt. Nachbargebäude entwickeln sich gemeinsam weiter; zerstörte Gebiete werden wiederaufgebaut.

Assets/Scenes/City Prototype.unity in Unity 6000.0.83f1 mit URP 17.0.4 öffnen. Dies ist ein Unity-Prototyp; die Browserausführung wurde nicht geprüft.

Über die Stadt ziehen und loslassen, um die Formation in diese Richtung zu starten. Flugzeuge fliegen von außerhalb des Bildes nach außerhalb. 1: Bombe, 2: Nuklear, Leertaste: Pause/Weiter, Esc: Abbruch.

Jedes Flugzeug startet mit einer Bombe pro Sekunde. Abwurfrate, Bombenlast, Radius, Schaden, Formation, Tempo, Panzerung und Leben sind verbesserbar. Flugzeugverbesserungen gelten ab dem nächsten Einsatz. Stadtwachstum wird nicht mehr gekauft.

Das Wachstum beschleunigt sich automatisch mit der Spielzeit: anfangs 1x, nach 60 Sekunden 2x, nach 420 Sekunden höchstens 8x. Das HUD zeigt den Wert. Nur Gebäude ab Stufe 3 feuern Raketen. Jeder zwölfte Neubau ist ein gelbes Treibstofflager, rotes Munitionslager oder türkisfarbenes Kraftwerk. Zerstörung verursacht Umgebungsschaden und Kettenreaktionen. Gold wird einmal für die ganze Kette ausgezahlt; Entwicklung sinkt und Ruinen werden wiederaufgebaut.

Jedes angekommene Flugzeug gibt feste 50% normale Einsatzladung. Zwei Ankünfte erlauben einen Einsatz; Überschüsse bleiben erhalten. Passives Laden dauert 10 Spielsekunden und füllt höchstens einen Einsatz.

Nuklearangriffe haben eine eigene Abklingzeit von 100 Sekunden ab Annahme eines gültigen Befehls, auch in der Warteschlange. Anfangs bereit, ohne normale Einsatzladung zu verbrauchen. Ankunftsboni und 3x-Tempo verkürzen sie nicht. Pause hält sie an; Neue Stadt setzt sie zurück.

Gebäude feuern stufenabhängige Raketen mit begrenzter Verfolgung. Flugzeuge können beschädigt und abgeschossen werden. Größere Gebäude haben mehr Leben und können durch Treffer schrumpfen. Zerstörung gibt Gold und senkt die Stadtentwicklung.

Beim ersten Start gilt die Systemsprache. Der Sprachknopf wechselt zwischen System und neun Sprachen; die Auswahl wird lokal gespeichert. Nicht unterstützte Sprachen nutzen Englisch.

## Steuerung und Einstellungen

Externe Originale unter Assets/polyperfect/Low Poly Epic City/ dürfen nicht verändert werden. Eigene Assets und Adapter verwenden. Store-Assets nicht separat weitergeben.

Grenzen: Unity-Darstellung, Sprachwechsel im Spiel, Web-Bereitstellung und Balance wurden hier nicht geprüft. Kompilierung und unabhängige Tests ersetzen keinen Play-Test.

[HTML](index.html)

Explosive Gebäude haben nun einen leuchtend blauen Baukörper; gelbe, rote und türkise Fundamente kennzeichnen den Typ. Die Zerstörung kombiniert seitliches Kippen, verdrehten Einsturz und Explosionshub. Fassadenteile und kleine Trümmer fliegen physikalisch, teils über Dächer oder aus dem Bild. Höchstens 36 Strukturteile teilen das bestehende Trümmerbudget. Die neuen Effekte und der Fragment-Shader benötigen noch einen echten Play-Test.

Die Stadt startet mit96 belegten Feldern, Kombinationen und Spezialgebäuden. Der Standardsamen ergibt in der unabhängigen Simulation82 Gebäude. Pro Wachstum entstehen bis zu4 Felder an getrennten, verbundenen Rändern nach gewichteter Zufallswahl statt Zentralabstand. Die Grenze beträgt24×24, insgesamt576 Felder (bei Feldgröße16 etwa384×384Unity-Einheiten). Volle Karten expandieren nicht weiter, zerstörte Flächen werden aber wiederaufgebaut. Zwischen90 und300 Spielsekunden steigt der Anteil direkt gebauter hoher Stufen. Ab300 Sekunden gelten für normale Neu-/Wiederaufbauten75% Stufe3,20% Stufe2 und5% Basis. Spezialgebäude behalten eigene Regeln. Neue Stadt setzt den Fortschritt zurück.
