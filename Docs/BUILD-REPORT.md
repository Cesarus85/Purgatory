# Quest Demon MR – Build- und Prüfbericht V18–V20

Stand: 12. September 2026. Ältere Einträge unterhalb V20.6 sind historisch.

## V20.6 — Abgestufte Katana-Kontakte und sichere nahe Portal-Austritte

**0.20.6/code66 gebaut und vollständig lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.6-contact-portals.apk`, 141.443.160 Bytes, SHA-256 `8d678bc17b2e0c50ed5c0cf6f685384276bbaf0de5dcad309f33f8b6ff36f85c`. V20.5-Erkennung bleibt erhalten; leichte Kontakte erzeugen kleine Wunden und wenig Schaden, bewusste Hiebe/Stiche denselben vollen Grundschaden. Aktuelle Bewegung und kurzer Median begrenzen Geschwindigkeitsspitzen. Zusätzlich langsamen Stich-Bildratenrandfall korrigiert. Nahe Portalöffnung getrennt vom erneut geprüften Austritt; kurze seitliche Gehwege, Körper-/Möbelprüfung, lokales Warten und begrenztes Verlegen statt pauschalem großen Spielerabstand.

1.740 neue native Prüfschritte plus vollständige Vorgänger-Regressionskette bestanden, einschließlich echter Hautwunden und realem Portal-Traversal mit Spieler-/Möbelblockaden. Bilder der Streifwunden geprüft. 140 eingefrorene Quellen, Start-/Versionskonfiguration, APK-v2-Signatur/ZIP/ARM64/Metadaten verifiziert; abschließender Verifizierer Exit 0. Export `ZAYHhf`, Paketierung `xHqAsK`, Gradle 9m49s. SDK-/Editorwarnungen und langsame Shader-Cache-Dateizugriffe dokumentiert. [Lieferbericht und Testfolge](V20.6-CONTACT-AND-CLOSE-PORTALS.md), [Paketnachweis](../Verification/V20.6/delivery.json). Getragene Quest-Abnahme bleibt offen.

## V20.5 — Verlässliche Katana-Schlagfolgen und echte Hautkontakte

**0.20.5/code65 gebaut und vollständig lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.5-katana-reliability.apk`, 141.437.716 Bytes, SHA-256 `6eef6667de1548bec644c449d00ed7b17532716754046a23a1c9ecc9efd573a6`. Bewusste Wechselhiebe werden früher und ohne alte Gegnersperre erkannt; stillgehaltene Klinge/Zittern bleiben ohne Schaden. Kurzer Anlauf wird geometrisch nachgeprüft, aktuelle Kontaktbewegung bleibt zwingend. Ein nachgewiesener Normalisierungsfehler kleiner Hautdreiecke erzeugte falsche Kontakte und dadurch unsichtbare Wunden; korrigiert und gezielt abgesichert. Grundschaden, Perk-Auslöser, Klingenhaltung und stummer Schwungton unverändert.

1.138 neue native Prüfschritte plus vollständige V20.4/V20.3/V20.2/V20.1/V20/V19/Shotgun/Händigkeit/Blut-Regressionskette bestanden. Produktions-Mehrfachhiebe gegen bewegte Dämonen und Fledermäuse: beide Richtungen, horizontal/diagonal, je Schaden/Ladung/frische sichtbare Wunde; native Bildvergleiche isoliert und über älteren Wunden. 138 eingefrorene Dateien, Versions-/Startkonfiguration, APK-v2-Signatur/ZIP/ARM64/Metadaten verifiziert; abschließender Verifizierer Exit 0. Export `3WTrvk`, Paketierung `a92oKu`, Gradle 14m01s. SDK-/Editorwarnungen und langsame ausgelagerte Dateizugriffe dokumentiert. [Lieferbericht und Testfolge](V20.5-KATANA-RELIABILITY.md), [Paketnachweis](../Verification/V20.5/delivery.json). Getragene Quest-Abnahme bleibt offen.

## V20.4 — Gleichwertige Katana-Hiebe/Stiche, sichtbare Wunden und Siegel-Nachschub

**0.20.4/code64 gebaut und vollständig lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.4-katana-seal-polish.apk`, 141.432.496 Bytes, SHA-256 `de2a1cb6f2e327600876d0fcf0f8d25330e99b58846048bbedd68f2f1cacefbb`. Bewusste Hiebe/Stiche mit gleichem Grundschaden, Handgelenkrotation berücksichtigt; bloße Berührung, Zittern, Halten, Pause und Tracking-Sprünge bleiben abgesichert. Schnittwunden besser lesbar und maximal 0,65 mm entlang der animierten Hautnormalen angehoben. Schwunggeräusch vollständig stumm, Kontakt/Parade erhalten. Siegelportale warten bei kurzzeitig blockiertem Austritt, zeigen lokale Hinweise und verlagern nach sechs aktiven Sekunden anhaltender Nähe/Geometrieblockade ausschließlich den fehlenden Gegner; zerstörte Siegel verhindern Nachschub weiterhin.

3.001 neue parametrisierte Prüfschritte plus V20.3/V20.2/V20.1/V20/V19/Shotgun/Händigkeit/Blut-Regressionen bestanden. Echte Produktions-Handgelenk-Hiebe beider Richtungen, native Wundbilder und tatsächlicher zweiter Austritt aus demselben Portal geprüft. 146 Quellen einschließlich PlayerSettings vor/nach Export hashidentisch, APK-v2-Signatur/ZIP/ARM64/Metadaten/Startkonfiguration verifiziert. Export `bU8vMT`, frische Paketierung `eKfs0X`, Gradle 10m23s. Ausgelagertes Prüfskript blockierte nach erfolgreichem Build; gleichwertiger lokaler Verifizierer mit sämtlichen Bedingungen anschließend Exit 0. [Lieferbericht und Testfolge](V20.4-KATANA-SEAL-POLISH.md), [Paketnachweis](../Verification/V20.4/delivery.json). Getragene Quest-Abnahme bleibt offen.

## V20.3 — Robuste Stiche, bewegungsabhängiger Schaden und mittige Fledermaus-Austritte

**0.20.3/code63 gebaut und lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.3-katana-contact-fixes.apk`, 141.429.556 Bytes, SHA-256 `59cca7806bd76c7640d2ad7a4429c393470dccad1384613c19ca189f08448996`. Zeitfensterbasierte Stich-/Schnittabsicht, aktuelle Kontaktenergie und abgestufter Schaden verhindern volle Treffer durch bloßes Berühren. Neuer kurzer Feuer-/Plasma-Parierklang ohne Metallresonanz. Beide Fledermaus-Eintrittsvarianten beginnen an der Aperturmitte und durchqueren diese vor dem seitlichen Abbiegen. Akzeptierte Klingenhaltung und Perk-Auslöser unverändert. 1.706 neue parametrisierte Prüfschritte sowie sämtliche aufgerufenen V20.2/V20.1/V20/V19/Shotgun/Händigkeit/Blut-Regressionen bestanden; wackelige Produktionsstiche und nachlaufender zweiter Gegner ausdrücklich geprüft. Hautgebundene Einstichwunde visuell kontrolliert. 145 Vor-Export-Dateien nach Build exakt unverändert, APK-Signatur/ZIP/ARM64/Metadaten/XR-Konfiguration geprüft. Gradle 10m22s; kompletter finaler Verifizierer Exitcode 0 nach vorübergehendem Protokoll-Lesetimeout. [Lieferbericht und Testfolge](V20.3-KATANA-CONTACT-FIXES.md), [Paketnachweis](../Verification/V20.3/delivery.json). Reale Klang-/Kontakt-/Stereo-Abnahme bleibt offen.

## V20.2 — Klingenhaltung, Katana-Audio, Spitzenstiche und aktivere Fernkämpfer

**0.20.2/code62 gebaut und lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.2-katana-combat-polish.apk`, 141.417.871 Bytes, SHA-256 `45ef71f1ef3cfef190f7ffbdda13c0bcffbc672d0e262a2e52756a299efbe4a5`. Klingenfläche am unveränderten Blender-Original vermessen und gemeinsam mit der Spitze ausgerichtet; Griffanker bewahrt. Gedämpfte tiefe Parade, räumlicher geschwindigkeitsabhängiger Schwungton, separate Spitzenstiche mit kleiner dunkler hautgebundener Wunde, kurzem Trefferimpuls und weniger Blut/Rückstoß. Fernkämpfer werfen häufiger und geben blockierte Distanzhaltung an den Anlauf/Nahkampf zurück. Portalverteilung unverändert. 982 neue parametrisierte Prüfungen sowie sämtliche V20.1/V20/V19/Shotgun/Händigkeit/Blut-Regressionen bestanden. Signatur, ZIP, ARM64/IL2CPP, neue Laufzeittypen und 143 eingefrorene Dateien geprüft; während des Exports geänderte Projekteinstellungen separat exakt gegen V20.1 plus Versionswerte validiert, XR-Bootkonfiguration und Manifeste unabhängig verglichen. Endexport `o5ZIc8`, Paketierung `1zQlQO`, Gradle 14m01s. [Lieferbericht und Testfolge](V20.2-KATANA-COMBAT-POLISH.md), [Paketnachweis](../Verification/V20.2/delivery.json). Reale Klang-, Bewegungs- und Stereo-Abnahme auf Quest bleibt offen.

## V20.1 — Katana-Griff, tödliche Schnitte und Boden-Navigation korrigiert

**0.20.1/code61 gebaut und vollständig lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.1-katana-fixes.apk`, 141.389.548 Bytes, SHA-256 `d6369b0edfe19d908a0102720bac472b6d0c61f6c646bd3b2aa8e9924990e94e`. Bestehende Signatur, ARM64/IL2CPP, ZIP, Laufzeittypen und 141 Quellstände geprüft. Tatsächlichen Blender-Mesh-/Socket-Versatz korrigiert; Griff und sichtbare Treffergeometrie erstmals gegeneinander vermessen. Tödliche Schnittspuren deutlicher und hautgebunden, geringerer Klingen-Todesimpuls; Nahkampf-Zielradius, Gruppen-Ausweichen, Kreiserkennung, Umweg-Baseline und Slot-Neuzuordnung korrigiert. NarrowWall-Prüfung verwendet nicht-schwere Körperbreite; rückwärtige Ablehnungsgründe werden protokolliert. Finale 168 neue Checks plus sämtliche bisherigen V20/V19/Shotgun/Handrollen/Blut-Regressionen bestanden. Frischer Endexport `lVLenO`, frische Paketierung `rl8MuS`, 26m11s. V20.1-Headset-Abnahme bleibt offen. [Lieferbericht und Testfolge](V20.1-CORRECTIONS.md), [Paketnachweis](../Verification/V20.1/delivery.json).

## V20.0 — Katana, sichtbare Schnitte und Bestiarium gebaut

**0.20.0/code60 gebaut und lokal verifiziert, nicht installiert.** APK `Builds/Purgatory-v20.0-katana.apk`, 141.411.664 Bytes, SHA-256 `87146d3c6f8a586aed0b929e9c04dc82dfd607c34cb589dda6b4fb26b53cdaf7`. ARM64/IL2CPP, bestehende APK-v2-Signatur, neue Laufzeittypen, ZIP und eingefrorener Quellstand geprüft. Nutzer-Katana in Blender optimiert/neu gebacken, gemeinsame Perk-Steuerung mit Shotgun-Priorität, kurze kontinuierliche Hautschnitte, sichtbare animierte Wunden, Parade, Audio/Haptik, Testknopf, Varianten und Kettenbüßer ergänzt. Finale 577 V20-, 228 V19-, 138 Shotgun-, 30 Handrollen- und 14 Blutchecks erfolgreich; zusätzliche native Ensemble-Bilder gesichtet. Clean-Export mit bewahrtem Startfix, keine TypeDB-Doppelregistrierung im Endexport. Frische Android-Paketierung erfolgreich in 10m06s. Quest blieb aus: reale V19-/V20-Spiel-/Stereo-/Leistungsabnahme offen. [Lieferbericht](V20-DELIVERY.md), [gemeinsame Testliste](V19-V20-TESTPLAN.md), [Paketnachweis](../Verification/V20/delivery.json).

## V19.20 — Startkorrektur nach auf Quest reproduziertem V19.19-Absturz

**V19.20 / code59 installiert und Startkorrektur auf Quest bestätigt.** Nutzerstart plus zwei kontrollierte Kaltstarts erreichen die vollständige Initialisierung ohne erneuten Absturz; jeweils 37 Assets, keine fehlenden. Vier Raumdateien nach Update hashidentisch. APK `Builds/Purgatory-v19.20-startup-fix.apk`, 134.828.830 Bytes, SHA-256 `749ffe6e41fc538b52e520b5b2f554dd395ff25cc73bd33b07f1639985131847`; ZIP, Metadaten und bestehende APK-v2-Signatur geprüft. V19.19 war trotz Editorchecks beim nativen Start abgestürzt: 101 widersprüchliche generierte Typdatenkopien wurden wiederherstellbar ausgelagert, Diagnosefeld vom Szenenformat getrennt und Spieler/Startszene/APK sauber neu gebaut. Finale 228 V19-Checks und Szenenformatprüfung bestanden, keine TypeDB-Doppelregistrierungen mehr. V19-D-Spiel-/Langzeitabnahme bleibt offen. [Abschließender Liefernachweis](V19-20-DELIVERY.md), [Ursachenbefund](V19-20-STARTUP-FIX.md).

## V19.19 — Schnellneustart / Raumverteilung / Kampfregie gebaut, nicht installiert

- APK: `Builds/Purgatory-v19.19-combat-director.apk`, 134.829.218 Bytes; **0.19.19/code58**, ARM64/IL2CPP, min29/target36, nicht debuggable. SHA-256 `776db13751b2109e8e1bd301005b52366d87aa7dc41827e6d51ddfd80cc03059`.
- Gültige Raumsitzung/Schrein beim Rundenneustart behalten, klare Menüaktion mit neutraler Eingabesicherung; raumlokale Portal-Kandidaten und feinere begrenzte Navigation; koordinierter Kampfdruck, bestehende Rollen, echte Brust-Schwachstelle und begrenzter Feuerball-Abfangbonus. Gemeinsamer Raumabfrage-Cache, optionale Kandidatendiagnose und P99 ergänzt.
- Finale native Gesamtsuite: **4.128 CHECK-Meldungen**, darunter **228 V19-Checks**; zusätzlich 36 unabhängige Statistik-/Logchecks und fünf Python-Tests. Unity-Export und Gradle erfolgreich (3m29s), APK-ZIP, Version/Architektur und vorhandene APK-v2-Entwicklungssignatur geprüft. Keine C#-/Shaderfehler oder Exceptions im finalen Export; bestehende TypeDB-/Editor-/Gradle-Warnungen bleiben.
- Native Bilder der Bruststelle geprüft. V19.18-APK und bestehendes Dämonen-FBX nach SHA-256 unverändert. Keine Quest verbunden; nichts installiert oder gestartet. **V19-D-Hardwareabnahme bleibt offen**, insbesondere reale hintere Nische, Eingabegefühl, Kampfdruck und aktuelle Langzeit-/Thermik-/Framezeiten.
- [Umfang und Testfolge](V19-19-COMPLETION.md), [Paketnachweis](../Verification/V19Completion/delivery.json), [Gesamtplan](IMPLEMENTIERUNGSKONZEPT-V19-V23.md).

## V19.9 – Kaltstartkorrektur gebaut, Installation und Headset-Abnahme offen

- **V19.8 ist nach Nutzerfeedback nicht bestanden:** erster Ladeversuch richtig, nach App-Neustart wieder verschoben. Im echten Mitschnitt bleibt die Karte unverändert (128 Chunks, GPU=0), aber die alte Prüfung meldet trotzdem 48 passende Punkte. Keine belegte quantitative Ankerpose im alten Log.
- **0.19.9 / code48**, Purgatory, Paket unverändert; `Builds/QuestDemonMR-v19.9-cold-start.apk`, 106.625.194 Bytes. SHA-256 `cb101be1e1362fb7a6a8d9bd35e1026349b7bb69b1607e1350f98ef4764994f1`, APK-v2 mit bestehendem Entwicklungsschlüssel geprüft, ARM64/IL2CPP min29/target36. Kein Store-Release.
- Direkter nativer OVRAnchor-Lebenszyklus statt kamerabasierter OVRSpatialAnchor-Weltpose. Expliziter OVRCameraRig.trackingSpace für Erzeugung und Lokalisierung, so wie vom lokalen SDK gegen veraltete Kamera-Zeitpunkte empfohlen. Stabile späte Korrekturen richten die ungeprüfte Karte neu aus und verwerfen alte Prüfdaten. Zusätzlich Boden und zwei nicht parallele Wandnormalen erforderlich. Zwei begrenzte lokale Sitzungsprotokolle überdauern App-Ende; Nutzerraum unverändert.
- **2.483 CHECK-Meldungen** im finalen Export, einschließlich 26 zusätzlicher Kaltstart-/Dreiflächen-Fälle. Keine C#-/Shader-/Exception-Fehler; bekannte SDK-/Editor-Warnungen bleiben. Gradle 4m 14s, 117 Tasks. Export `/private/tmp/qdmr-v199-export.3aSSe4`.
- **Nicht installiert oder gestartet:** Freigabe zur V19.9-Installation im laufenden Task noch unbeantwortet. Gespeicherte Datei auf Quest weiterhin hashidentisch, vorherige APK erhalten. Zwei echte Kaltstarts mit vorhandener Karte und Diagnosevergleich bleiben notwendig; der SDK-Timingbefund ist plausibel und konkret, aber noch nicht als Ursache dieses physischen Versatzes bestätigt.
- [Befund und Umsetzung](IMPLEMENTATION-V19-9-COLD-START.md), [Liefernachweis](../Verification/ColdStart/delivery-v19.9.txt). Frühere Zustandsmeldungen folgen historisch.

## V19.8 – Raum-Zuverlässigkeit: installiert, Headset-Abnahme offen

- **0.19.8 / versionCode 47**, Purgatory, Paket unverändert. APK `Builds/QuestDemonMR-v19.8-room-reliability.apk`, 106.611.798 Bytes, ARM64/IL2CPP, min29/target36. SHA-256 `bce1c1e24f0898e10e6dc30869c0ff669df5caa2d2b9d7289597041036d7c8c8`; APK-v2 mit bestehendem Entwicklungsschlüssel geprüft, kein Store-Release.
- Gespeicherte TSDF-Karte erhält einen eigenen starren Koordinatenrahmen; der Kamera-Rig wird beim Laden nicht mehr verschoben. CPU-Abfragen, Collider/Mesh und GPU-Integration verwenden denselben Rahmen. Stabile verfolgte Meta-Ankerpose vor Speichern/Laden, kanonische Anker-/Schrein-/Bodenmetadaten beim erneuten Speichern. V1-Dateien bleiben lesbar.
- Getrennte, abbrechbare Prüfung ohne zweite Rekonstruktion; explizites RAUM VERWENDEN nach Tiefenabgleich, dann echte Wiederverwendung der gespeicherten Karte. Ungeeigneter Standort bleibt in der Prüfung. Verfallende Fehlmessungen statt dauerhaft verbrauchter Abgleichzähler. Fehler, Recenter, Fokus-Rückkehr und Ankerverlust führen sauber zur Raumwahl. Veränderte unbeobachtete Möbel sind keine garantierte Live-Erkennung; bei größeren Änderungen neu scannen.
- **2.457 CHECK-Meldungen im vollständigen nativen Export**, darunter 103 neue Prüfungen. Versetzung/Yaw 0/45/90/178 Grad, reale MeshCollider-/Freiraumabfragen, echte Metal-Compute-Tiefenintegration, unveränderte tatsächliche Nutzerdatei, Wieder-Speichern, negativer Standort, Recenter/Fokus, Timeout und verschachtelter Ladefehler. Abschließender fokussierter Wiederholungslauf 103 + 50 Checks bestanden; nicht nochmals zur Gesamtsumme addiert.
- Vollständiger Export enthält genau den absichtlich injizierten Test-Ladefehler, keine unerwarteten C#-/Shader-/Exception-Fehler. Finaler Fokuslauf mit expliziter Erwartungsmarkierung ohne solche Fehler. Erster Export wegen fehlendem V198-Pfadguard verworfen; Guard korrigiert. Separater Build-Treiber-Quotingfehler korrigiert und Syntax geprüft; finaler Export unabhängig paketiert. Gradle 2m 9s, 117 Tasks. Vorläufe erhalten; bekannte Editor-/SDK-Warnungen einschließlich schon bei V19.7 vorhandener Teardown-Allokationswarnung bleiben.
- **Auf Quest 3 installiert**, `2G0YC5ZG9609PY`, Gerätezeit 2026-09-07 20:04:37. Version, APK-Hash und erteilte Raum-/Ankerberechtigungen bestätigt. Vorhandenes Profil mit 128 Chunks vor/nach Update hashidentisch, lokale Sicherung vorhanden; nicht in APK eingebettet. Keine Gerätedaten gelöscht und nicht automatisch gestartet. Getragener Save/Quit/Load-Zyklus mit anderer Startpose sowie Import-/Freigabe-Frametimes bleiben offen.
- [Ablauf, Grenzen und genaue Headset-Testfolge](IMPLEMENTATION-V19-8-ROOM-RELIABILITY.md), [Liefernachweis](../Verification/RoomReliability/delivery-v19.8.txt). Die folgenden Einträge sind historische Stände.

## V19.7 – Purgatory: gebaut, Installation noch offen

- **0.19.7 / versionCode 46**, Anzeigename **Purgatory**, unverändertes Paket `de.stefanmaier.questdemonmr`. ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.7-purgatory-setup.apk`, **106.607.226 Bytes**.
- APK-SHA-256: `25455bd4fbed69274414ce973c7d1cd071a18f0b503743b037d79845025ce789`. `libil2cpp.so`: `81773053bef47ac9912b6f542e0f1889a76e617a395482b309d285c22a96bc8a`. APK-v2 mit bestehendem Entwicklungsschlüssel verifiziert; Anchor-API-Berechtigung im Paket, kein Store-Release.
- Neues begrenztes Startpanel mit echten Ziel-/Abzug-Schaltflächen und optionalem Stick-/Tastenzugriff. Direkter `RAUM SPEICHERN`-Knopf am Schrein mit automatischem freien Platz bzw. Aktualisierung des aktiven Raums; bewusste Bestätigung bei voller Liste. Verifizierte Speicherung und sichtbare Erfolgsrückmeldung auf dem Knopf. Exklusive Phasenanzeigen statt übereinanderliegender Scan-/Spiel-/Einrichtungstexte. Name auch am Schrein und im Android-Launcher.
- Bodenaustritte heben früher über die Schwelle, nutzen dieselbe dichter geprüfte Vorab-/Laufzeitkurve und tolerieren einzelne wechselnde Tiefenmessungen durch kurzes Anhalten statt sofortigem Rückzug. Tatsächliche Blockaden bleiben gesperrt. Fledermaus-Abbruch unverändert; konkrete Nutzerbeobachtung mangels passendem Geräteereignis nicht vollständig reproduziert.
- **2.287 CHECK-Meldungen im vollständigen finalen nativen Exportlauf**, einschließlich 52 neuer SetupFlow-Prüfungen. Danach **acht zusätzliche Raumprofilprüfungen** mit editorseitig aktualisierter Menükontrolle, ohne weitere Laufzeitänderung: insgesamt 2.295. Drei native Menüansichten angesehen, leer/ein Startpanel mit mehreren Profilen/volle Fünferliste, jeweils synthetische Daten.
- Export `/private/tmp/qdmr-v197-export.SvMkY1`; Gradle **2m 44s**, 117 Tasks (34 ausgeführt, 83 aktuell). Finale C#-/Shader-/Exception-Suche ohne Treffer. Früher Compilerfehler im Exportpfad-Guard behoben, schwarze Fontatlas-RGB-Abtastung in der Bildprüfung korrigiert, zu weit auf Deckenstürze ausgedehnte Debounce-Regel nach Regression begrenzt. Zwischenlauf für Menü-Neuausrichtung angehalten. Vorläufe erhalten; bekannte SDK-/TypeDB-/Gradle-/Editor-Teardown-Warnungen bleiben.
- **Gerät nur gelesen, nicht installiert:** Quest 3 `2G0YC5ZG9609PY`, V19.6/code45 sowie erteilte Raum-/Ankerberechtigungen bestätigt. Im lesbaren App-Dateiverzeichnis fehlt der konfigurierte Ordner `room-profiles`; dort bislang kein gespeichertes Profil beobachtet. Keine passende Portal-/Profilmeldung in den letzten 6.000 Logcat-Einträgen. Installationsfrage offen; keine App gestartet, keine Gerätedaten verändert. Vorherige APK unverändert.
- [Bedienung, Umsetzung und Headset-Testfolge](IMPLEMENTATION-V19-7-SETUP.md), [Liefernachweis](../Verification/SetupFlow/delivery-v19.7.txt), [Startmenü](../Verification/SetupFlow/start-left.png). Echter Save/Load-/Relokalisierungszyklus, Controllerkomfort, räumliche Fußfreiheit und Framezeiten bleiben offen.

## V19.6 – gebaut, noch nicht installiert: Raumwahl und Wurfkomfort

- **0.19.6 / versionCode 45**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.6-comfort-recovery.apk`, **106.596.808 Bytes**.
- APK-SHA-256: `35d074566fd72ede0f3bf7d60e96558c5f58238a167e8f332adf4ea6773d7a77`. `libil2cpp.so`: `390fc2af3876288605a47ea10187a289455019fca942c026830ad097d33097fc`. APK-v2 mit bestehendem Entwicklungsschlüssel und Anchor-API-Berechtigung geprüft; kein Store-Release.
- Tatsächliche Profildateizahl statt Aktionszähler, gespeicherte Räume zuerst, keine Rekonstruktion im Auswahlmenü, überprüftes Speichern und separate vollständige goldene Referenzvorschau. Aktuelle Ausrichtung und Freiraumprüfung bleiben erforderlich. Sichtbare Landung/Rückkehr bei sicherheitsbedingt abgebrochenem Austritt. Handgelenkvorrat statt Hüftschätzung, freie Grip-Aufnahme und kurze unterstützte Würfe; ruhiges Loslassen gibt zurück.
- **2.234 native CHECK-Meldungen im finalen vollständigen Exportlauf**, davon 19 ComfortRecovery, 23 Raumprofil-, 51 Stern- und 30 Handrollen-Checks. Zusätzlich drei native Ansichten angesehen: leeres und gespeichertes Linkshänder-Menü sowie Goldraster-Shadermuster; keine Raum-/Headsetaufnahme. Separater gezielter 19-Check-Vorlauf nicht zur finalen Summe addiert.
- Finaler Export `/private/tmp/qdmr-v196-export.9rajvg`, Gradle erfolgreich in **46 Sekunden**, 117 Tasks (20 ausgeführt, 97 aktuell). Finale C#-/Shader-/Exception-Suche in Export und Bildprüfung ohne Treffer. Erste Zwischenfassung erhalten; Vulkan-Fehler durch reservierten Shader-Variablennamen im zweiten Export erkannt, Paketbau gestoppt, Variable korrigiert und vollständigen Export wiederholt. Bestehende Editor-/SDK-/TypeDB-/Gradle- sowie Persistent-Allocation-Teardown-Warnungen bleiben; kein warnungsfreier Lauf behauptet.
- **Nicht installiert:** aktuelle ADB-Liste leer. Keine Gerätedaten verändert, keine App gestartet; V19.5-APK unverändert. Der Nutzer hat einen neueren Stand selbst getestet; ohne Geräteabfrage wird keine aktuell installierte Version behauptet.
- [Umsetzung und konkrete Testfolge](REVIEW-V19-6.md), [Liefernachweis](../Verification/ComfortRecovery/delivery-v19.6.txt), [gespeicherter Raum im Menü](../Verification/ComfortRecovery/menu-saved-left.png). Offen: tatsächliche Meta-Anker-Speicherung/Relokalisierung, vorhandene Nutzerprofildatei, räumliche Abdeckung, blockierte Rückzugswege, getragenes Wurfgefühl und Framezeiten.

## V19.5 – gebaut, noch nicht installiert: lokale Raumprofile

- **0.19.5 / versionCode 44**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.5-room-profiles.apk`, **106.569.646 Bytes**.
- APK-SHA-256: `a31c7ff963ef88072416ddc94b77be161995c9bf1da5c7d0fd019670d3cd9f63`. `libil2cpp.so`: `87582ff09be400be4b4ec8bf0807f0263392d70cbafc175cab03662ccd4fd935`. APK-v2-Signatur mit einem Signierer und bestehendem Entwicklungsschlüssel geprüft. `com.oculus.permission.USE_ANCHOR_API` im finalen Paket bestätigt; kein Store-Release.
- Fünf lokale Raumprofile mit eigenem TSDF, persistiertem Meta-Anker, Ankerpose und optionaler Schreinpose. Komprimierte, geprüfte, atomar ersetzte Dateien; Profilmenü vor dem Scan und am Pause-Schrein. Rollenbasierte Tasten und vorgehaltene Eingaben abgesichert. Geladene Freisamples zunächst unbekannt; neue Tiefendaten müssen Ausrichtung sowie relevante Flächen/Freiraum bestätigen. Eigener Live-Scan bleibt, keine Meta-Scene-Karte als Ersatz.
- **2.215 native CHECK-Meldungen im vollständigen Exportlauf**, darunter 23 neue Profil-, 30 Handrollen-, 51 Stern- und 523 RhythmLife-Checks. Danach **acht zusätzliche gezielte Editorchecks** ohne Änderung des exportierten Laufzeitcodes: mehrere Richtungen erforderlich/ausreichend, Ausrichtung allein ersetzt lokale Freifläche nicht, Recenter bricht Import ab, korrekt beschriftetes Linkshänder-Menü. Native Menüansicht angesehen. Zusammen 2.223 CHECK-Meldungen, keine Behauptung physischer Meta-Anker-Tests.
- Finale Export-/Zusatzlogs ohne `error CS`, `Shader error` oder `Exception:`. Bestehende Editor-Destroy-/Material-, SDK-/TypeDB-/Gradle-Warnungen bleiben. Erster Compilerlauf mit mehrdeutigem CompressionLevel korrigiert. Ein zufälliger Randfehlschuss im älteren Siegeltest führte zu reproduzierbarem Seed und regulärem Zweihand-Schuss im Test, nicht zu veränderter Produktionsstreuung. Zusatztest zuerst mit überlappenden, halben Voxelpositionen; auf eindeutige Rasterpunkte korrigiert. Frühere Logs erhalten.
- Export `/private/tmp/qdmr-v195-export.uYfFfZ`; Gradle erfolgreich in **7m 44s**, 117 Tasks (29 ausgeführt, 88 aktuell). Skripte `Tools/build-v19.5.sh` / `package-v19.5.sh`. Zusatztest `RoomProfilesValidation.ExtraValidation` separat nach Export; rein Editor-seitig.
- **Nicht installiert:** ADB-Geräteliste am 7. September 2026 leer. Keine App gestartet und keine Gerätedaten verändert. V19.4-APK per ursprünglichem SHA unverändert bestätigt; letzter verifiziert installierter Stand bleibt V19.2.
- [Bedienung, Datenhaltung und offene Abnahme](IMPLEMENTATION-ROOM-PROFILES.md), [Liefernachweis](../Verification/RoomProfiles/delivery-v19.5.txt), [geprüfte Menüansicht](../Verification/RoomProfiles/menu-left.png). Unbedingt offen: echter Save/Load/Relocalize-Zyklus nach App-Neustart, anderer Startpunkt, falscher Raum, verschobene Möbel, Schrein-Wiederherstellung und Quest-Framezeiten.

## V19.4 – gebaut, noch nicht installiert: gespeicherte Waffenhand

- **0.19.4 / versionCode 43**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.4-hand-roles.apk`, **106.510.918 Bytes**.
- APK-SHA-256: `04e6ad72753d307c893d8e5673671413070194e3fc25a39fcb534a21a9bbb240`. `libil2cpp.so`: `b1b124094cbec24861cd7a5af4c0beb8758ef86cb5b5827488fbec0be2e29dba`. APK-v2-Signatur, ein Signierer mit bestehendem Entwicklungsschlüssel geprüft; kein Store-Release.
- Vor dem ersten Scan gespeicherte Waffenhandwahl per zwei Sekunden Abzug der gewünschten Hand, späterer Wechsel am Pause-Schrein. Rollenbasierte Geräte, Anker, Haptik, Sternhalter und Tasten; kompakte Hinweise passend umgerechnet. Getrennte Waffenjustierung je Seite, keine negative Modellskalierung. Eingabesperre bis beide Controller neutral/getrackt sind. Kein Rundenvorrats- oder Raumreset beim Wechsel.
- **2.191 native CHECK-Meldungen**, einschließlich **30 neuer Handrollen-Checks**, 51 Wurfstern- und 523 RhythmLife-Checks. Beide Seiten, Speicherung, tatsächliche Controlleranker, Kampfsperre, echter pausierter Wechsel, erhaltene Raumrevision/Munition/Regeneration, Rückgabe gehaltener Sterne, Scan-Halten gegen Diagnosekombination und gerenderte Tastenhinweise geprüft. Die Erstauswahl mit tatsächlichen Touch-Controllern bleibt physisch offen.
- Frischer Export `/private/tmp/qdmr-v194-export.xsU5zQ`; Gradle erfolgreich in **2m 32s**, 117 Tasks (29 ausgeführt, 88 aktuell). Finale C#-/Shader-/Exception-Suche ohne Treffer. Bestehende Editor-Destroy-/Material-, SDK-/TypeDB-/Gradle-Warnungen bleiben. Erster Compilerlauf mit zwei neuen Test-/Exportguard-Fehlern korrigiert; Logs erhalten. Ein weiterer Lauf deckte eine unzulässige Erwartung des zufälligen Nachschubtests auf: Ein breiterer Nachfolger kann am für den schlanken ersten Gegner geeigneten Austritt korrekt scheitern. Test erwartet jetzt bei gemessen fehlender Freigängigkeit Abbruch/Retry, ohne Produktions-Raumprüfung zu ändern; finaler kompletter Lauf bestanden.
- **Nicht installiert:** ADB-Geräteliste am 7. September 2026 leer. Keine App gestartet, keine Gerätedaten verändert. V19.3-APK per ursprünglichem SHA unverändert bestätigt; V19.2 bleibt letzter verifiziert installierter Stand.
- [Bedienung und offene Headset-Abnahme](IMPLEMENTATION-HAND-ROLES.md), [Liefernachweis](../Verification/HandRoles/delivery-v19.4.txt). Linke Waffenpose, rechte Hüfte/Wurfbewegung, Scan-/Schreinbedienung und Performance müssen getragen geprüft werden.

## V19.3 – gebaut, noch nicht installiert: zusätzliche Wurfsterne

- **0.19.3 / versionCode 42**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.3-throwing-stars.apk`, **106.509.066 Bytes**.
- APK-SHA-256: `d7ae0b833b5eafa298c5b241d75f49866cfafc6d0dc298d81775ed73ddc8b204`. `libil2cpp.so`: `d95dd5d83e3db564f976fedc0d36b7ef4a693b03d6ac4de672a2f690d6c76c6e`. APK-v2-Signatur mit einem Signierer und bestehendem Entwicklungsschlüssel geprüft; kein Store-Release.
- Geliefertes FBX in Blender geprüft und unverändert übernommen: 9.724 Quelldreiecke, 9.716 in Unity, ca. elf Zentimeter diagonal. Vier korrekt gepackte PBR-Maps, ein gemeinsames Material. Finale native Modellansicht angesehen. Downloads-Originale nicht verändert.
- **2.160 native CHECK-Meldungen**, darunter **51 Wurfstern-Prüfungen**. Vollständige funktionale RhythmLife-Regressionskette, aktualisierte Startup-Assetprüfung, Vorrat/150-Sekunden-Regeneration, echte Grip-/Loslass-Routine, Pause/Trackingverlust, Geschwindigkeitsverlauf, dünne Wand, tatsächliche deformierte Monsteroberfläche, einmaliger Schaden, schwacher Fallwurf, Lebensdauer und Reset geprüft. V19.2s zwei zusätzlichen Countdown-Bildprüfungen wurden nicht erneut aufgerufen; deren funktionale Regeln sind in RhythmLife enthalten.
- Finaler Unity-Export `/private/tmp/qdmr-v193-export.nxCdFW`; Gradle erfolgreich in **3m 5s**, 117 Tasks (29 ausgeführt, 88 aktuell). Keine finalen C#-/Shader-/Exception-Treffer. Bekannte SDK-/Gradle-/TypeDB-Warnungen sowie eine Editor-Destroy-Warnung aus bestehendem Treffer-VFX im synthetischen Monstertest; keine Behauptung eines warnungsfreien Laufs. Frühere fehlgeschlagene Assetzahl-/Import-Erwartungen korrigiert und durch vollständigen finalen Lauf ersetzt.
- **Nicht installiert:** `adb devices -l` am 7. September 2026 ohne Gerät. V19.2 bleibt der letzte verifiziert installierte Stand. Keine App gestartet, keine Gerätedaten verändert, ältere APKs erhalten.
- [Umsetzung und Bedienung](IMPLEMENTATION-THROWING-STARS.md), [Liefernachweis](../Verification/ThrowingStars/delivery-v19.3.txt), [Modellansicht](../Verification/ThrowingStars/star-unity.png). Getragene Hüft-/Pose-/Timing-/Wurfweiten- und Performanceabnahme offen.

## V19.2 – installiert: Siegelverteilung und klarer Ablauf

- **0.19.2 / versionCode 41**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.2-seal-clarity.apk`, 105.312.174 Bytes.
- SHA-256: `7d5c9f3ce164fa44e7eaaaffaaee445e5480668464068cc00a95c4d95dbb22e6`. APK-v2-Signatur mit bestehendem Entwicklungsschlüssel geprüft, ein Signierer. Kein Store-Release behauptet.
- **2.106 native CHECK-Meldungen**, darunter 523 im erweiterten RhythmLife-Paket und zwei neue gerenderte Countdown-Ansichten. Verteilung über 100 Wellen, Produktionsfälle für Wellen 3–5, echte Schüsse, beide Ergebnisse, kein falsches Durchlassen bei abgebrochenem Austritt, Drei-Sekunden-Ablauf, sofortiges Entfernen nach Erfolg, Pause und Schriftorientierung geprüft. Bestehende gesamte Regression enthalten. Beide Countdown-Ansichten angesehen.
- Frischer finaler Export `/private/tmp/qdmr-v192-export.vYA89B`; Gradle erfolgreich in 5m 10s, 117 Tasks (29 ausgeführt, 88 aktuell). Keine finalen C#/Shader-/Exception-Treffer. Bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben. Ein erster Lauf wurde bewusst vor Export für eine zusätzliche Countdown-Korrektur beendet und separat aufbewahrt.
- Quest 3 `2G0YC5ZG9609PY`: `adb install -r` erfolgreich am **6. September 2026 um 22:31:40**. Geräteversion und Oculus/Horizon-Szene-/Anchor-Berechtigungen bestätigt. Daten erhalten, nicht gestartet; V19.1-APK unverändert erhalten.
- [Umsetzung, Bilder und Abnahmegrenzen](REVIEW-V19-2.md), [Liefernachweis](../Verification/SealClarity/delivery-v19.2.txt). Kein neuer Hauptblock begonnen. Getragene Lesbarkeits-/Raum-/Rhythmusabnahme bleibt offen.
- Installierte `base.apk` per Geräte-SHA-256 bestätigt: identisch mit dem obigen lokalen Paket.

## V19.1 – installiert: optionaler Nachschub-Eingriff und lebendigere Gegner

- **0.19.1 / versionCode 40**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.1-rhythm-life.apk`, 105.311.070 Bytes.
- SHA-256: `10079a100258101f70c704b922032bbb216e056211d17b84a54bd5f3cda0c33c`. APK-v2-Signatur mit bestehendem Entwicklungsschlüssel geprüft; ein Signierer. Kein Store-Release behauptet.
- **1.936 native CHECK-Meldungen**, darunter 355 neue RhythmLife- und 25 Portal-Art-Prüfungen. Beide optionalen Siegelentscheidungen mit Produktionsroutinen/Schüssen, Munition, Quoten, Abbruch/Retry, Pause, Reset-Accounting und Animationsdrift geprüft. Alle deformierten Wundvertices gegen tatsächliches Mesh verglichen. Bestehende Scan-/Knie-/Sturz-/Kampf-/Audio-/Reliktregression bestanden, finale aktuelle Unity-Posen angesehen.
- Frischer Export `/private/tmp/qdmr-v191-export.56yimO`; Gradle erfolgreich in 12m 47s, 117 Tasks (29 ausgeführt, 88 aktuell). Keine finalen C#/Shader-/Exception-Treffer. Bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben.
- Quest 3 `2G0YC5ZG9609PY`: `adb install -r` erfolgreich am **6. September 2026 um 22:01:51**. Geräteversion und Oculus/Horizon-Szene-/Anchor-Berechtigungen bestätigt. Daten erhalten, nicht gestartet; V19.0-APK unverändert erhalten.
- [Umsetzung, Bilder und offene Abnahme](REVIEW-V19-1.md), [Liefernachweis](../Verification/RhythmLife/delivery-v19.1.txt). Pflicht-Siegelgate ausdrücklich ersetzt. Getragene Rhythmus-/Animations-/Performanceabnahme sowie übriger V19-Ausbau bleiben offen.
- Installierte `base.apk` per Geräte-SHA-256 geprüft: identisch mit dem obigen lokalen Paket.

## V19.0 – installiert: Kathedrale, Rahmen und Portalversiegelung

- **0.19.0 / versionCode 39**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v19.0-portal-sealing.apk`, 105.710.938 Bytes.
- SHA-256: `ebd0557ff7eb09d5d1825eefae46908469bfb988cf0d8a478d93e490781cc2fa`. APK-v2-Signatur mit bestehendem Entwicklungsschlüssel geprüft, ein Signierer; Geräte-APK stimmt exakt überein. Kein Store-Release behauptet.
- **1.737 native CHECK-Meldungen**, davon 181 neue PortalSealing-Fälle. Gesamte V18.20-Regression, Blender-Import/Budgets, echte Ortsfolge und aufrechter Schacht, Raum-/Sichtprüfung, echte Zweier-Spawn-Routine und Waffenschüsse, Pause, Hindernisse, endliche Siegel, einmalige Zusatzmunition und Schließen geprüft. Final gerenderte Unity-Bilder angesehen. Material-Teardown im Edit-Modus explizit aufgerufen.
- Frischer Export `/private/tmp/qdmr-v190-export.V3Abxa`; Gradle erfolgreich in 3m 51s, 117 Tasks (29 ausgeführt, 88 aktuell). Keine finalen C#/Shader-/Exception-Treffer; bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben.
- Quest 3 `2G0YC5ZG9609PY`: `adb install -r` erfolgreich am **6. September 2026 um 20:48:28**. Geräteversion/Hash und Oculus/Horizon-Szene-/Anchor-Berechtigungen bestätigt. Daten erhalten, nicht gestartet. V18.20 als vorheriges APK unverändert erhalten.
- [Umsetzung/Bilder und Grenzen](REVIEW-V19-0.md), [Liefernachweis](../Verification/PortalSealing/delivery-v19.0.txt). A10-Art implementiert, physische Qualität/Performance offen. Portalversiegelung ist der erste V19-Spielblock, nicht das gesamte V19-Ziel.

## V18.20 – installiert: begrenzte Scan-Lückentoleranz und gezielte Hinweise

- **0.18.20 / versionCode 38**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v18.20-scan-gaps.apk`, 101.845.202 Bytes.
- SHA-256: `527834c255a81a5cf900833ca7a3a961e2d27022520280063966c226f6a059ec`. APK-v2-Signatur geprüft; installierte Geräte-APK stimmt exakt überein.
- **1.550 native CHECK-Meldungen**, davon 33 neue ScanGaps-Fälle: isolierte Lücken, tatsächliche Körper-/Wegeprüfungen, unveränderte Rohdaten, keine rekursiven Ergänzungen, Roh-/Collider-Hindernisse, Chunk-Grenzen, Boden-/Wandlöcher mit umliegenden echten Treffern, Abgründe/unbekannte Rückseiten, größere Lücken und Höhenwechsel, konkrete Hinweise. V18.19-Regressionskette enthalten.
- Frischer Export `/private/tmp/qdmr-v1820-export.rMYHjH`; Gradle erfolgreich in 2m 56s, 117 Tasks (29 ausgeführt, 88 aktuell). Keine finalen C#/Shader-/Exception-Treffer; bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben.
- Quest 3 `2G0YC5ZG9609PY`: `adb install -r` erfolgreich am **6. September 2026 um 20:07:23**. Version, Hash und Oculus/Horizon-Szene-/Anchor-Berechtigungen bestätigt. Daten erhalten, nicht automatisch gestartet.
- [Umsetzung und Grenzen](REVIEW-V18-20.md), [Liefernachweis](../Verification/ScanGaps/delivery-v18.20.txt). Rohnetz darf kleine Lücken behalten; keine Zusage geschlossener Geometrie oder Laser-Tag-Gleichwertigkeit. Reale Scan-Erleichterung und Quest-Framezeiten noch offen. V18.19 unverändert erhalten.

## V18.19 – installiert: Zwei-Sekunden-X, Kniehebung, senkrechter Kopfvoran-Sturz

- **0.18.19 / versionCode 37**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. APK: `Builds/QuestDemonMR-v18.19-knee-dive.apk`, 101.839.926 Bytes.
- SHA-256: `3ad7b27852c8c89de94f379df02c268cbe9e2c8bc2c55250a99a7ca4c98969da`. APK-v2-Signatur mit einem Signierer geprüft; Geräte-APK stimmt exakt überein.
- **1.517 native CHECK-Meldungen**, darunter 52 neue KneeDive-Prüfungen und 58 angepasste Schwellen-/Setup-Prüfungen. Reale Knie-/Krallen-Geometrie, Zweitastenkonflikte, Vorhalten, echte senkrechte Kieferachse, gekrümmter Flug und neu auftauchendes Hindernis geprüft. Finale Unity-Bilder angesehen; kein getragener Bewegungstest.
- Blender-Schritt und Raum-Fußführung gemeinsam überarbeitet. Scan per X-Halten für zwei Sekunden mit Fortschritt, ohne Loslass-Geste. Fledermaus stürzt senkrecht kopfvoran und zieht in sicheren Anflug; Normalflug-Fallback bleibt. Scan-Messung/Streaming unverändert.
- Frischer Export `/private/tmp/qdmr-v1819-export.mQDWQx`; Gradle erfolgreich in 1m 43s, 117 Tasks (29 ausgeführt, 88 aktuell). Keine finalen C#/Shader-/Exception-Treffer; bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben.
- Quest 3 `2G0YC5ZG9609PY`: `adb install -r` erfolgreich, **6. September 2026, 19:19:33**. Version, Hash und Oculus/Horizon-Szene-/Anchor-Berechtigungen bestätigt. Daten erhalten, nicht automatisch gestartet.
- [Details und Headset-Abnahme](REVIEW-V18-19.md), [Liefernachweis](../Verification/KneeDive/delivery-v18.19.txt). Vorversion V18.18 unverändert erhalten; A10-Kathedrale und ortsbezogene Rahmen weiterhin offen.

## V18.18 – installiert: Schwellenübertritt, Fledermaus-Auswahl, manuelle Scan-Freigabe

- **0.18.18 / versionCode 36**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v18.18-threshold-setup.apk`, **101.838.462 Bytes**; AAPT und APK-v2-Signatur mit einem Signer bestätigt.
- APK-SHA-256 `df5de37a0bce104a4ceac951f700ff52dda3bab93080e5685f3f303a2afaac2d`; `libil2cpp.so` SHA-256 `4e9c5e3c5e73b0e7d3df8cbaefbabc79d85a0592a3af642ae8823bb73d8e453f`. V18.17-APK unverändert per ursprünglichem SHA bestätigt.
- **1.465 native CHECK-Meldungen**, darunter **58 neue ThresholdSetup-Prüfungen**, vollständige frühere Regression. Tatsächlicher Fledermaus-Auswahlweg über die zuvor widersprüchliche Decken-Bedingung bis zum kopfüber Akteur, Mindestscan/Connected-Sektoren, frische X-Haltebestätigung, X+Y-/Kurz-/Vorabdruck, kein Auto-Wechsel, erhaltene Karte und Unbekanntheit, Reset; tatsächliche Fußziele und Skin-/Krallenfreiraum bei mehreren Größen und voller/schmaler/kompakter Öffnung sowie Treffer während des Schritts geprüft. Keine Aussage über 1.465 unabhängige physische Tests.
- Überarbeiteter Blender-PortalStep bei erhaltenen Sprung-/Kampfclips; Laufzeit-Fußziele an reale Portaldistanz und anatomische Hüftseiten angepasst, kein zusätzlicher Mesh-Bake im Spiel. Neue echte X-Bestätigung vor Schreinplatzierung; Scan aktualisiert weiterhin. [Umfang, Bedienung, Grenzen](REVIEW-V18-18.md).
- Finaler Export `/private/tmp/qdmr-v1818-export.NhztRF`, Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 42s**, 117 Tasks, 29 ausgeführt/88 unverändert. Skripte `Tools/build-v18.18.sh` / `package-v18.18.sh` syntaxgeprüft. Logs unter `Verification/ThresholdSetup/`; finaler C#-/Shader-/Exception-Filter ohne Treffer, bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben. Frühere Test-/Animationsstände samt Logs erhalten. Zeit-/Abstands- und Setup-Erwartungen in früheren Tests an den neuen Vertrag angepasst, Sicherheits-/Poseprüfungen erhalten.
- **Installiert am 6. September 2026, 18:38:59** auf Quest 3 `2G0YC5ZG9609PY`: `adb install -r` Success, Geräteversion 0.18.18/36 und installierter base.apk-SHA exakt bestätigt. Scene-/Anchor-Berechtigungen für Oculus/Horizon erhalten, keine App-Daten gelöscht, nicht automatisch gestartet. [Prüfnachweis](../Verification/ThresholdSetup/delivery-v18.18.txt). Getragene Eingabe-/Schritt-/Fledermaus-/Performanceabnahme offen; kein zusätzlicher realer Scan-Stillstand ohne Laufprotokoll als behoben behauptet.

## V18.17 – installiert: fortgesetzter Scan, Schacht und Austrittstempo

- **0.18.17 / versionCode 35**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v18.17-scan-shaft.apk`, **101.825.486 Bytes**; AAPT und APK-v2-Signatur mit einem Signer bestätigt.
- APK-SHA-256 `e0b15dd08680f1ffb8b980e4b45b3f37dee70e4ebe04353316a2614bc8ac856a`; `libil2cpp.so` SHA-256 `dbddb47b505f512b9d0dd723db4d22998e4518e152802a59d4ad2107a6fcb408`. V18.15- und V18.16-APKs unverändert per ursprünglichem SHA bestätigt.
- **1.405 native CHECK-Meldungen**, darunter **32 neue Expansion-Prüfungen** und die 70 V18.16-Arrival-Prüfungen in der vollständigen bisherigen Regressionskette. Zweistufiges Speicherbudget, Grenze über 256, geschützte Residenz/Archivbereiche, GPU-Restore, Recycling/Unbekanntheit/Neuentdeckung/Reset; vertikale Modell-/Mappingachsen, native Ansichten und getrennte Boden-Ortsauswahl geprüft. Keine Ableitung tatsächlicher Quest-Frametimes oder vollständiger physischer Raumabdeckung.
- Blender-Schacht **25.826 Dreiecke / vier Asset-Materialslots / 2048²-Atlas**, ASTC 6×6. Finale Sichtprüfung führte zu schmalerem Schacht und kleinerer ferner Glutöffnung für sichtbar gestaffelte Tiefe; Sky/Frame/Effekte zusätzliche Renderarbeit. Enthält V18.16-Brücke und Bewegungsvarianten; normale Auftritte beschleunigt, mehrere geprüfte Sprunglandestrecken. [Umfang und Grenzen](REVIEW-V18-17.md).
- Finaler Export `/private/tmp/qdmr-v1817-export.wyXbqf`; Paketierung `/private/tmp/qdmr-v184-package.SE7n42`; **BUILD SUCCESSFUL in 43s**, 117 Tasks, 20 ausgeführt/97 unverändert. Skripte `Tools/build-v18.17.sh` / `package-v18.17.sh` syntaxgeprüft; finale Logs und Nachweis unter `Verification/ScanExpansion/`. Kein C#-/Shader-/Exception-Treffer im finalen Fehlerfilter, bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben. Frühere Test-/Artkandidaten samt Logs archiviert, nicht installiert.
- **Installiert am 6. September 2026, 17:14:15** auf Quest 3 `2G0YC5ZG9609PY`: `adb install -r` Success, Version 0.18.17/35 und installierter base.apk-SHA exakt bestätigt. Scene-/Anchor-Berechtigungen für Oculus und Horizon erhalten. Keine App-Daten gelöscht, nicht automatisch gestartet. Vorher lag tatsächlich V18.15/33 auf dem Gerät, nicht V18.16. [Nachweis](../Verification/ScanExpansion/delivery-v18.17.txt). Getragener Scan-/Tempo-/Stereo-/Performance-Test weiterhin offen.

## V18.16 – gebaut, noch nicht installiert: A10b-Teil und Ankunftsvarianten

- **0.18.16 / versionCode 34**, ARM64/IL2CPP, Release ohne Debuggable, min29/target36. `Builds/QuestDemonMR-v18.16-arrival-variants.apk`, **99.633.022 Bytes**; AAPT und APK-v2-Signatur mit einem Signer bestätigt.
- APK-SHA-256 `66da764e43e1158a7320720341b96ea27c31c140804687f122abaaf0c38687cf`; `libil2cpp.so` SHA-256 `80037a354fec7a9423154408a8f006df60a0adfeeb0294e75a314ff0306a7f22`. V18.15-APK unverändert mit unten genanntem SHA bestätigt.
- **1.371 native CHECK-Meldungen**, darunter **70 neue Arrival-Prüfungen**, vollständige frühere Regressionskette und erweiterte Clip-/Assetfälle. Geprüfte positive/negative Raumkurven, Spielerabstände, tatsächliche Trefferunterbrechung, Landung nach neuem Hindernis, beide Austrittsvarianten, Normal-Fallback, sechs verschiedene aufeinanderfolgende Produktionsorte und bereinigte Endpose. Native Art-/Animationsbilder angesehen; kein physischer Stereo-/Frametime-Beweis.
- Originale Blender-Brücke: **14.676 Dreiecke, vier Asset-Materialslots, eigener 2048²-Atlas**; zusätzlich ein Sky-Dome-Material/Draw pro Portalauge. Neue Blender-Clips Leap/InvertedBurst, vorhandene Animationen erhalten. Schmiede und Brücke alternieren; Kathedrale und individuelle Ortsrahmen noch offen. [Umfang und Grenzen](REVIEW-V18-16.md).
- Finaler Export `/private/tmp/qdmr-v1816-export.zpme6H`, Paketierung `/private/tmp/qdmr-v184-package.SE7n42`; **BUILD SUCCESSFUL in 2m 40s**, 117 Tasks, 29 ausgeführt/88 unverändert. `Tools/build-v18.16.sh` und `Tools/package-v18.16.sh` syntaxgeprüft. Logs `Verification/ArrivalVariants/unity-export.log` / `build-driver.log`; Nachweis `delivery-v18.16.txt` dort. Erfolgsmarker für Blender, native Prüfung und Android-Export bestätigt.
- Früherer vollständiger Lauf erkannte PelvicCurl-Leakage in InvertedBurst; Blender-Clip korrigiert und gesamte Kette erneut erfolgreich geprüft, alte Invariante erhalten. Fehllogs unter `before-wing-fold` archiviert. Bekannte TypeDB-/SDK-/Gradle-Warnungen nicht als beseitigt ausgewiesen.
- **Nicht installiert oder gestartet.** Keine App-Daten-/Berechtigungsänderung. Letzte verifizierte Installation bleibt V18.15. Nutzer bestätigt diesen Vorgänger allgemein positiv; V18.16 benötigt noch getragenen Test von Art, Sprungtempo, Abstand, Rollen und Performance.

## V18.15 – installiert: A10a und Startperformance-Nachtrag

- **0.18.15 / versionCode 33**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable, min29/target36. APK `Builds/QuestDemonMR-v18.15-portal-entry.apk`, **97.977.980 Bytes**.
- SHA-256 **`80e0fb10ad04ca885e21edd5b0634fa4d226559b8fb3cdfcbba61e3ac4745e9a`**. AAPT-Metadaten, APK-v2-Signatur und ein Signer bestätigt; `libil2cpp.so`: `de91a4815f9fd4379f589243b36410b36b332b83a507c3dbde008066e94693a3`.
- **1.291 native CHECK-Meldungen**, davon **64 neue A10a-/Startlast-Prüfungen**, plus vollständige frühere Regressionskette mit den neuen Clip-/Ressourcenfällen. Produktiver Lebens-/Munitionsdrop, sichere Bodenauflage, erhaltene Pity bei unbekannter Fläche, wirklicher animierter Mesh-Treffer durch das offene Fenster, keine Ausnahme vor vorgelagerten Möbeln, Deckenanflug, Abbruch/Cleanup, reine Renderkopie, Pause-Zeit und Start-/Reset-Gate geprüft. Native Ansichten frontal/seitlich/Peek/Decke angesehen. Das beweist weder getragene Stereowirkung noch erreichte Quest-Frametime.
- Blender-Schmiede **15.184 Dreiecke / vier Slots / 2048²-Atlas mit gebackener Spaltenverschattung**, neue `PortalStep`-/`Peek`-Takes bei Erhalt der alten Clips. Echte animierte Austritte, zwei unabhängige Versorgungszähler und bevorzugte Fußbodenauflage. Erst **ein** Schauplatz; A10b mit zwei weiteren Orten und ortsspezifischen Rahmen nach Qualitätsabnahme. [Umfang/Grenzen](REVIEW-V18-15.md), [Implementierungsplan](A10-IMPLEMENTATION.md).
- Nutzer-Ruckelmeldung: Überlappende Vorbereitung/Rekonstruktion/Schreinplatzierung im Code bestätigt, tatsächlicher Hauptverursacher ohne Quest-Trace nicht bewiesen. Startphasen getrennt; Scanvergleich auf Worker, Commit-/Readback-Frequenzen während Platzierung reduziert, sichtbares Scangitter standardmäßig aus bei erhaltener Depth-Verdeckung. Messmarker und zehnsekündige Kostenprotokolle hinzugefügt. **Keine gemessene Performanceverbesserung behauptet.**
- Finaler Export `/private/tmp/qdmr-v1815-export.WxLIPB`; Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 3s**, 117 Tasks, 29 ausgeführt/88 unverändert. Skripte `Tools/build-v18.15.sh`, `package-v18.15.sh`; Shellsyntax geprüft. Finale Logs `Verification/PortalEntry/unity-export.log` / `build-driver.log`. Keine C#-/Shader-/Exception-Fehler im finalen Export; bekannte TypeDB-/SDK-/Gradle-Warnungen und Editor-Allokationen nicht als behoben ausgegeben. Ein früher fehlgeschlagener Exportlauf ist unter `unity-export-before-scan-gate-test.log` erhalten: alte Fokusprüfung erwartete sofortige Platzierung und wurde auf das neue Scan-Gate bei weiter ungültiger Bestätigung angepasst.
- **Installiert am 6. September 2026, 16:05:48** auf Quest 3 `2G0YC5ZG9609PY`: `adb install -r` Success, Geräteversion 0.18.15/33 und installierter base.apk-SHA exakt bestätigt. Scene-/Anchor-Berechtigungen erhalten, keine App-Daten gelöscht, nicht automatisch gestartet. V18.14-APK bei Buildlieferung per ursprünglichem SHA unverändert bestätigt. Prüfnachweis `Verification/PortalEntry/delivery-v18.15.txt`. Worn-Headset-Kaltstart, Scan/Platzierung, Lebensversorgung und Portalübergänge bleiben offen; hintere Bodenportalverteilung weiter zurückgestellt.

## V18.14 – installiert: A9 Relikte und Aufnahmeeffekte

- **0.18.14 / versionCode 32**, ARM64/IL2CPP/Vulkan, kein Development/Debuggable, min29/target36. APK `Builds/QuestDemonMR-v18.14-relics.apk`, **96.251.158 Bytes**.
- SHA-256 **`6c4601838afd2b1d065f0787e0eeb482d2c78026a860ae9a4b5386c03e94bba3`**; AAPT-Metadaten und APK-v2-Signatur verifiziert, ein Signer. `libil2cpp.so`: `baca33dcba5ae8c69ef22190e7a96003f17d80b507d407a8496443599ebc7cac`.
- **1.217 native CHECK-Meldungen** im finalen Validierungs-/Exportlauf, davon **59 neue A9-Prüfungen** plus drei neue Warmup-Ressourcen. Exakte tatsächliche Ressourcenänderung, volle Vorräte, leerer Revolver und Nachladen im produktiven Triggerpfad, Einmaligkeit, Raum-/Sofakollision, unbekannte Samples, niedrige Möbelkante, FBX-Achsenerhalt und Pool-/Audiocleanup geprüft. Vollständige bisherige Regressionskette bestanden. Keine getragene Geräteabnahme daraus abgeleitet.
- Originale Blender-Relikte: Patronenkranz 5.694 Dreiecke, Lebensgefäß 3.208; jeweils vier Materialslots und ein gemeinsamer 1024²-Atlas. FBX-Roundtrip, kompakte native Maße und Materialzuordnung geprüft. Getrennte Meshpartikel für Glut und Seelenzug, vier gepoolte Stimmen, kompakte echte Bonusanzeige, passende Ton-/Haptikrückmeldung. Neue Vollvolumenprüfung umfasst Drehung und Schweben. Revolver, Kampfklang-Mischung, normale Drop-Mengen/-Wahrscheinlichkeiten und Scan-Architektur unverändert.
- Finaler Export `/private/tmp/qdmr-v1814-export.TwdXAV`, Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 43s**, 117 Tasks, 20 ausgeführt, 97 unverändert. Reproduzierbare Skripte `Tools/build-v18.14.sh` / `package-v18.14.sh`; Logs unter `Verification/Relics/`. Kein C#-/Shaderfehler im finalen Exportlog; bestehende TypeDB-/SDK-/Gradle-Warnungen und zwei persistente Editor-Allokationen bleiben.
- **Installiert am 6. September 2026, 13:12:06** auf Quest 3 `2G0YC5ZG9609PY`: `adb install -r` Success, Version 0.18.14/32 und installierter base.apk-Hash exakt bestätigt. Beide Scene-Berechtigungen erhalten. Keine App-Daten gelöscht, nicht automatisch gestartet. V18.13-APK unverändert per ursprünglichem SHA bestätigt; zwei nicht installierte Zwischenkandidaten samt Logs archiviert.
- [Umsetzung und Grenzen](REVIEW-V18-14.md), [Testplan](A9-TEST-PLAN.md), kompakter Nachweis `Verification/Relics/delivery-v18.14.txt`. Luna Max lieferte Asset-/Testgrundlage und Codeprüfung; Hauptagent Integration, Korrekturen, visuelle/native QA und Lieferung. Getragene Aufnahme-/Möbel-/Sicht-/Hör-/Haptik-/Frametime-Abnahme offen. Nächster Block: **A10 Portal-Schauplätze**, zuerst eine Qualitätsreferenz. Hintere Bodenportalverteilung bleibt zurückgestellt.

## V18.13 – installiert: A8 Ritualschrein und freie Platzierung

- **0.18.13 / versionCode 31**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable, min29/target36. APK `Builds/QuestDemonMR-v18.13-shrine.apk`, **95.509.002 Bytes**.
- SHA-256 **`ddf6ccdd4e2a98a4a0795bbab7f1c04d6f29ba28457c86784248aee9cba612e7`**. AAPT-Metadaten und APK-v2-Signatur verifiziert, ein Signer. `libil2cpp.so`: `f33ba22ca71c6b6ce41362067e0f99e21cb043b1d316e73353075b138512b1a3`.
- **1.155 native CHECK-Meldungen** im finalen Validierungs-/Exportlauf: 38 neue Regel-/Sitzungsprüfungen, 40 integrierte Prüfungen, zwei neue Warmup-Assets und ein zusätzlicher bestehender Audio-Menü-Setupcheck. Vollständige frühere Regressionskette erfolgreich. Bekannte synthetische Tiefenkarte und echte Collider prüfen produktiven Bestätigungspfad, kein Schuss/Start bei Platzierung, sichere Fläche/Volumen, Y-Pause, Abbrechen, Audio-Rückkehr, Positionsstabilität, Navigation, Reset, zerstörte Referenzen und Fokus-/Pause-Reihenfolgen.
- Originales Blender-Modell samt reproduzierbarem Skript und Quelle: 3.896 Dreiecke, vier Materialien, eine 1024²-Albedotextur, etwa 43 × 36 × 69 cm. Native Import-/Material-/Schriftseitenprüfungen bestanden; finale Unity- und Blender-Vorschauen angesehen unter `Verification/Shrine/`. Luna Max wurde für Asset und Testgrundlage sowie abschließende Codeprüfung eingesetzt; Hauptagent korrigierte, integrierte und prüfte die Lieferung. Keine gemessene Gesamt-Tokenersparnis behauptet.
- Finaler Export `/private/tmp/qdmr-v1813-export.GLntlk`; Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 59s**, 117 Tasks, 20 ausgeführt, 97 unverändert. Logs `Verification/Shrine/unity-export.log`, `build-driver.log`; Skripte `Tools/build-v18.13.sh`, `package-v18.13.sh`. Keine C#-/Shaderfehler im finalen Exportlog; bekannte TypeDB-/SDK-/Gradle-Warnungen und zwei persistente Editor-Allokationen bleiben.
- Vorläufiger Kandidat vor dem abschließenden Fokus-Guard: `Verification/Shrine/candidate-before-focus-guard.apk`, SHA `60088c902ac765da7778d7df827de9c0d3b6aff6a5f86b663447d4fe5053de04`, nicht installiert. Fehlgeschlagene Zwischenprüfungen und nachgearbeitete Legacy-Testaufbauten bleiben nachvollziehbar erhalten. V18.12-APK per ursprünglichem SHA unverändert bestätigt.
- **Installiert am 6. September 2026, 12:06:46:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; Version 0.18.13/31 und installierter base.apk-Hash exakt bestätigt. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht, nicht automatisch gestartet.
- Bedienung: rechter Strahl, Stick zum Drehen, Abzug zum Bestätigen, B zum Abbrechen. A in Spielpause zum Umplatzieren; Y links kurz zum Pausieren/Fortsetzen. KLANG öffnet das kompakte Audio-Untermenü. Nach Raum-/Trackingreset oder relevanter App-Unterbrechung neu platzieren. [Umfang und Grenzen](REVIEW-V18-13.md), [Testplan](A8-TEST-PLAN.md), kompakter Nachweis `Verification/Shrine/delivery-v18.13.txt`. Getragene Sicht-/Controller-/Flächen-/Fokus-/Frametime-Abnahme offen. Hintere Bodenportale zurückgestellt; nächster Sprintblock **A9 Relikte/Aufnahmeeffekte**.

## V18.12 – installiert: schmale Gänge und rückwärtige Bodenportale

- **0.18.12 / versionCode 30**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable, min29/target36. APK `Builds/QuestDemonMR-v18.12-rear-portals.apk`, **94.868.394 Bytes**.
- SHA-256 **`94e000f0d74eff5b88fff1c416d6598769aaf805c96fcd723dc842d5bdfa819b`**. AAPT-Metadaten und APK-v2-Signatur verifiziert, ein Signer. `libil2cpp.so`: `536623ec3abcff984b74e1577edb114fdb81cc39b6e1e37551f266c3909abf2c`.
- **1.074 native CHECK-Meldungen** im finalen Validierungs-/Exportlauf, davon 116 neue Rückseiten-/Gangprüfungen plus vier zusätzliche Rahmenprüfungen. Produktiver Selektor im synthetischen bekannten Raum mit 1,1 m breitem hinterem Gang und seitlichem Spielzeug: zwölf erfolgreiche Rückseitenanforderungen, sechs tiefer im Gang. Kompakter Austritt an schmaler Stirnwand bei 6,5 m Wandentfernung; vier sichere Ausweichplatzierungen bei quer versperrtem Gang; kein Spawn ohne bekannte Freiraumdaten. Vorherige Regressionskette erfolgreich.
- Rückseiten-Präferenz ab Welle 3/4/5 für ungefähr jeden vierten/dritten/zweiten regulären Boden-Slot, mit sicherem Fallback. 64 statt 48 Suchstrahlen, bis 7,5 m, feinere seitliche Alternativen, 1,06 m kompakter Rahmen ausschließlich für schlanken AshStalker. Bestehende Austrittsanimation gegen 81,2 cm Fensterbreite geprüft. Rückwärtige Bodenportale geben zwei Sekunden räumlichen Vorlauf. Keine neue Geometrie aus Blender, keine Änderung an Revolver, Munition oder Schussmischung.
- Export `/private/tmp/qdmr-v1812-export.KwCdar`; Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 47s**, 117 Tasks, 29 ausgeführt, 88 unverändert. Logs `Verification/RearPortals/unity-export.log`, `build-driver.log`; Skripte `Tools/build-v18.12.sh`, `package-v18.12.sh`, Shellsyntax geprüft. Keine C#-/Shaderfehler im finalen Exportlog; bekannte TypeDB-/SDK-/Gradle-Warnungen und zwei persistente Editor-Allokationen bleiben.
- **Installiert am 6. September 2026, 11:13:39:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; Version 0.18.12/30 und installierter base.apk-Hash exakt bestätigt. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht, nicht automatisch gestartet. V18.11-APK per ursprünglichem SHA unverändert bestätigt.
- [Befunde, Umsetzung und Grenzen](REVIEW-V18-12.md); Rohdaten `Verification/RearPortals/corridor-placements.csv`, kompakter Nachweis `delivery-v18.12.txt` im selben Ordner. Getragene Prüfung von tatsächlichem Gang, Hindernissen, Vorwarnung, sichtbarem Austritt und Such-Frametimes offen. Nächster Sprintblock bleibt A8, danach A9/A10.

## V18.11 – installiert: Raumnutzung, Suchhinweise und Revolvergröße

- **0.18.11 / versionCode 29**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable, min29/target36. APK `Builds/QuestDemonMR-v18.11-room-use.apk`, **94.867.826 Bytes**.
- SHA-256 **`c9a9435c16a3d796c965f2e1dfaba2b67c56ebd18d396560f5d7e2bfd4a5577f`**. AAPT-Metadaten und APK-v2-Signatur verifiziert, ein Signer. `libil2cpp.so`: `81fa5e45834f7044c42a14e3e145ebfe35acdacc5cc45e2a3277e687b122cc2c`.
- **954 native CHECK-Meldungen** im finalen Validierungs-/Exportlauf, davon **59 neue Raumnutzungs-/Größenprüfungen**. Produktiver Selektor im synthetisch bekannten Raum mit Sofa: 16 erfolgreiche Suchen, alle vier Wände, mindestens acht räumliche Zellen; sechs weitere erfolgreiche Suchen bei nur einem schmalen Wandstreifen. Kein Spawn nach Entfernen bekannter Freiraumdaten. Bisherige Kampf-/Audio-/Scan-/Animations-/Leerwellenprüfungen erneut erfolgreich.
- Der Revolver ist nochmals 20 % kleiner (0,90 → 0,72 des Originals), Griffposition unverändert, Mündung und Effektsocket deckungsgleich. Native Spieler-/Dreiviertelansichten angesehen und in `Verification/RoomUse/` gesichert. Kein neues Blender-Modell benötigt: bestehendes Modell und alle Mechanikteile gemeinsam skaliert.
- Export `/private/tmp/qdmr-v1811-export.wldkrS`; Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 5m 23s**, 117 Tasks, 29 ausgeführt, 88 unverändert. Größerer nativer Neuaufbau dauerte länger als die vorherige Korrektur. Logs `Verification/RoomUse/unity-export.log`, `build-driver.log`; Skripte `Tools/build-v18.11.sh`, `package-v18.11.sh`. Keine C#-/Shaderfehler im finalen Exportlog; bekannte TypeDB-/SDK-/Gradle-Warnungen und zwei persistente Editor-Allokationen weiterhin vorhanden.
- **Installiert am 6. September 2026, 10:46:52:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; Version 0.18.11/29 und installierter base.apk-Hash bestätigt. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht, nicht automatisch gestartet. Vorversion V18.10 per SHA unverändert bestätigt.
- [Befunde, Änderungen und Grenzen](REVIEW-V18-11.md), kompakter Nachweis `Verification/RoomUse/delivery-v18.11.txt`. Echte Raumverteilung, Frametimes und subjektive Waffengröße müssen mit getragenem Headset bestätigt werden. Als nächster Hauptblock dieses Sprints bleibt A8 (platzierbarer Ritualschrein).

## V18.10 – installiert: A7 Dynamik und kompakte Texte

- **0.18.10 / versionCode 28**, ARM64/IL2CPP/Vulkan, kein Development/Debuggable. APK `Builds/QuestDemonMR-v18.10-dynamics.apk`, **94.844.666 Bytes**.
- SHA-256 **`c4f8fa9bed4eab9220081eb53890a1a3adb5f5fec01b97b5881ef57e7b0952df`**. AAPT bestätigt Version, Paket, min29/target36 und ARM64; APK-v2-Signatur verifiziert, ein Signer. `libil2cpp.so`: `4d37ebbbd0d10dbb9c36ee728500cac481c5652f58f6e5d2f5b54ca9c8182125`.
- **895 CHECK-Zeilen** im finalen nativen Validierungs-/Exportlauf, einschließlich Build-Prüfungen; davon **74 A7-Prüfungen**. Gesamte frühere Regressionskette erneut durchlaufen, Gesichtsstabilität zusätzlich in HitAlt/Recover geprüft. Kein C#-/Shaderfehler im finalen Exportlog. Bestehende TypeDB-/SDK-/Gradle-Warnungen bleiben; keine daraus abgeleitete Geräte-Performanceabnahme.
- Blender-Quelle und FBX erstellt. Native Standphasenmessung der Fußknochen: **0,45–0,61 mm** Weltpositionsdrift bei gerader streckengebundener Bewegung, beide Beine und drei Größen; keine Behauptung vollständiger Fußfixierung bei realen Kurven/Übergängen. Neun finale Unity-Vorschaubilder für Gang/Treffer/HUD/Scan angesehen, zusätzlich Audio-Menü.
- Export `/private/tmp/qdmr-v1810-export.eKcPbi`; bekannte temporäre Paketierung `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 45s**, 117 Tasks, 20 ausgeführt, 97 unverändert. Reproduzierbare Skripte `Tools/build-v18.10.sh`, `Tools/package-v18.10.sh`; Logs `Verification/Dynamics/unity-export.log`, `build-driver.log`.
- Zwischenkandidat vor der zusätzlichen Scan-Textkorrektur bleibt als `Verification/Dynamics/candidate-before-scan-text.apk` erhalten (SHA `7750b1d25605499b1591928fd0c6782738de53feae1bfe2f22bc9bf9b0893037`), nicht installiert. Fehlgeschlagener erster Standphasentest und isolierte Nachprüfungen ebenfalls erhalten. Vorversionen V18.9, V18.8, V18.7 durch SHA-Vergleich unverändert bestätigt.
- **Installiert am 6. September 2026, 09:28:48:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; Geräteversion 0.18.10/28 bestätigt. Installierte base.apk hat exakt den oben angegebenen SHA-256. Scene-Berechtigungen weiterhin erteilt, keine App-Daten gelöscht, nicht automatisch gestartet. Der Export meldet weiterhin zwei persistente Editor-Allokationen; nicht als behoben behauptet.
- A6b vom Nutzer vorläufig positiv bestätigt. **Neue getragene A7-Abnahme offen:** Anlaufen/Kurven an Möbeln, Treffer-/Kampfübergänge, Flug und zentrale Textlesbarkeit. Ausführlicher [Änderungsbericht](REVIEW-V18-10.md). Nächster Funktions-/Artblock **A8: platzierbarer Ritualschrein**; A9/A10 bleiben geplant.

## V18.9 – installiert: A6b Schussdruck und Trefferzuordnung

- **0.18.9 / versionCode 27**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.9-shot-clarity.apk`, **94.806.670 Bytes**.
- SHA-256: `b040d7e195cab9f75b6460e38d74b6038ce7d65ec685993f265e192eb98cef2f`. AAPT bestätigt Paket/Version/min29/target36/ARM64; APK-v2-Signatur verifiziert, ein Signer.
- **813 native Prüfungen bestanden**, 748 Regressionen + 65 A6b-Prüfungen, genau 813 CHECK-Zeilen im finalen Exportlog. Erste 80 ms jedes Reports über 6 dB energiereicher, angeglichene Varianten, getrennte Kontakte, Summenbegrenzung, persistente unabhängige Regler, Mute, Priorität/Warnungsschutz, produktive Fire-/Kontaktkennung, Bedienung ohne Munitionsverbrauch sowie vollständige und abgebrochene Vorschaufolge.
- Unity-Steuerungsvorschau angesehen, zu große/überlappende Schrift korrigiert und erneut gerendert. Finale Aufnahme `Verification/ShotClarity/audio-controls.png`. Der erste Kandidat und fehlgeschlagene Zwischenprüfungen bleiben für Nachvollziehbarkeit erhalten und wurden nicht installiert. Ein Edit-Mode-Test musste die im Test fehlende OnEnable-Registrierung des Dämons explizit nachbilden; kein daraus abgeleiteter Laufzeit-Trefferfehler behauptet.
- Export `/private/tmp/qdmr-v189-export.H4vOEI`; Paketierung im bekannten temporären Arbeitsordner `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 8s**, 117 Tasks, 20 ausgeführt, 97 unverändert. Logs `Verification/ShotClarity/unity-export.log`, `build-package.log`. Keine C#-/Shaderfehler im finalen Lauf; bekannte TypeDB/SDK/Gradle-Warnungen und zwei persistente Editor-Allokationsmeldungen bleiben.
- `libil2cpp.so` SHA-256 `7e009b25c71c3ef2e5ddd0dfac75e6cf076943319fa582383c37d8d23bac46b6`. APK-Metadaten enthalten `Audio/CombatV18_9/`, `AudioMixPanel`, `CombatAudioAudit`, `ShotStarted`. Neuer 55-Clip-Bestand umfasst **1.192.650 WAV-Dateibytes**; 35 mechanische Clips unverändert kopiert, 20 Reports/Kontakte bearbeitet. V18.8 und V18.7 per ursprünglichen APK-Hashes unverändert bestätigt.
- **Installiert am 6. September 2026:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; Version 0.18.9 / Code 27, `lastUpdateTime=2026-09-06 08:55:12`. Installierte base.apk hat exakt den oben angegebenen SHA-256. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht, nicht automatisch gestartet. Hörabnahme weiterhin offen.
- Bedienung nach Installation: im pausierten Spiel **WAFFE −/+**, **TREFFER −/+** und **KLANGTEST / STOP** am Schrein mit Controller anvisieren und Abzug drücken. Standards 100 % Waffe / 70 % Treffer; Werte werden unabhängig gespeichert. Vorschau verbraucht keine Munition und beeinflusst keine Gegner. **Getragene Hörabnahme offen**, insbesondere Lautsprecher/Kopfhörer bei gleicher Systemlautstärke. Native Tests meldeten null Listener-Audio-Callbacks; ein aktiver Geräteausgang und dessen Summenpegel sind damit nicht vermessen.
- [Umsetzung, Befunde, Grenzen und Quellen](REVIEW-V18-9.md), kompakter Nachweis `Verification/ShotClarity/delivery-v18.9.txt`. Nach A6b-Hörabnahme folgt A7; A8–A10 bleiben geplant.

## V18.8 – installiert: A6 Gegnerklang, lautere Mischung und Portalaustritt

- **0.18.8 / versionCode 26**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.8-enemy-audio.apk`, **93.574.367 Bytes**.
- SHA-256: `f81873a7b704624a87c9ffa8d55fa16d443da5a0aacb0043c64a8de7159f5bd6`. AAPT bestätigt Paket/Version/min29/target36/ARM64; APK-v2-Signatur verifiziert, ein Signer.
- **748 native Prüfungen bestanden**, 564 bestehende + 184 A6-Prüfungen. Der finale Export enthält genau 748 erfolgreiche CHECK-Zeilen. Audioformat/PCM/Peaks/DC, Auswahlvarianten, Stimmenlimit, Prioritäten, Pause/Tod/Reset, lautere Gains, Limiter-DSP, Portalnormalen in acht Richtungen und importierte Gesichtsausrichtung während Emerge in vier Richtungen. Je Fuß und Bodengröße genau sechs Vorwärtsauftritte über drei Zwei-Schrittzyklen, keine bei fehlender tatsächlicher Fortbewegung.
- Die erweiterte Fußprüfung deckte zunächst doppelte Tritt-Cues auf. Die Blender-Quelldatei bestätigte zusätzliche Fußhebung beim Rückschwung; Hysterese plus Vorwärtsbedingung korrigieren den Audiotrigger. Fehlgeschlagene Zwischenläufe und der erste Kandidat bleiben unter `Verification/EnemyAudio/` erhalten, wurden nicht installiert. Alle 748 Prüfungen bestehen im finalen Lauf.
- 54 neue Mono-PCM-Clips, **4.418.184 WAV-Dateibytes**, acht globale gegnerbezogene AudioSources. A5-Aufnahmen und Revolvergröße unverändert; lautere Schuss-/Treffer-/Mechanik-Gains, listenerseitiger Limiter. [Umfang, Quellen und Grenzen](REVIEW-V18-8.md).
- Export `/private/tmp/qdmr-v188-export.snGb5M`; Paketierung im bekannten temporären Arbeitsordner `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 10s**, 117 Tasks, 20 ausgeführt, 97 unverändert. Log `Verification/EnemyAudio/build-package.log`; Unity-Export `Verification/EnemyAudio/unity-export.log`. Keine C#-/Shaderfehler im finalen Lauf; bekannte TypeDB-/SDK-/Gradle-Warnungen bleiben.
- Neue `libil2cpp.so` SHA-256 `ff16bdde87f9ab89ba215443e577f51e789a5afa3af79fe698f37b76fb1b6cac`. Vorherige V18.7-APK per unverändertem ursprünglichem Hash bestätigt.
- **Installiert:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`, `lastUpdateTime=2026-09-05 22:49:08`; installierte Version 0.18.8 / Code 26 und beide Scene-Berechtigungen bestätigt. Keine App-Daten gelöscht, nicht automatisch gestartet. Zwischenzeitlich blieb USB sichtbar, während ADB kein Gerät auflistete; einmaliger Serverneustart stellte die Verbindung wieder her.
- Getragene Hör-/Ortungs- und Austrittsabnahme, tatsächlicher Hardware-Ausgangsmix und neue Performance-/Thermikmessungen **offen**. Native Limiter-DSP-Tests sind keine Messung der Quest-Lautsprecher. Nächster Planpunkt: **A7 Dynamik/Integration**.
- SHA-256 der installierten `base.apk` stimmt exakt mit der Liefer-APK überein. Kompakter Nachweis: `Verification/EnemyAudio/delivery-v18.8.txt`. Die zwei bekannten persistenten Editor-Allokationsmeldungen bestehen weiterhin; kein Leak-Fix aus diesem Audio-Update abgeleitet.

## V18.7 – installiert: A5 Revolver-/Trefferklang und Größenkorrektur

- **0.18.7 / versionCode 25**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.7-audio.apk`, **89110817 Bytes**.
- SHA-256: `0a87c446db94c79fe815e5fb32c887141f39e140401b321c53ad8a1795e672ed`. AAPT bestätigt Version/min29/target36/ARM64, APK-v2-Signatur verifiziert, ein Signer.
- **564 native Prüfungen bestanden** in Vorprüfung und abschließendem Exportlauf: 401 Regressionen + 163 A5-Prüfungen. Log `Verification/AudioPolish/unity-export.log`; Unity-/Buildskript Exitcode 0. Bekannte SDK/TypeDB-Warnungen und zwei persistente Editor-Allokationen bleiben.
- 55 neue Mono-PCM-Varianten aus CC0-Revolveraufnahmen und gestalteten Kenney-CC0-Foley-Layern. Drei Schuss-, zwei Mechanik-, acht Trefferstimmen, phasengebundene Cues und Pausen-/Resetbehandlung. Revolver um 10 % bei konstantem Griffpunkt verkleinert. [Änderungen, Quellen, Grenzen und Prüfungen](REVIEW-V18-7.md).
- Export `/private/tmp/qdmr-v187-export.zlWy5T`; Paketierung im bekannten temporären Arbeitsordner `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 25s**, 117 Tasks, 29 ausgeführt, 88 unverändert. Log `Verification/AudioPolish/build-package.log`.
- Neue `libil2cpp.so` SHA-256 `0efd2d7cb5502943e3eab1b000a333f8c9e55b34413766da6fe3993a6ec87a83`. APK-Metadaten enthalten `Audio/CombatV18/`, `RevolverAudio`, `CombatImpactPause`, `RevolverReport`. V18.6 per ursprünglichem Hash unverändert bestätigt.
- **Installiert:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; `lastUpdateTime=2026-09-05 22:11:09`. Version 0.18.7 / Code 25 und installierte base.apk per obigem SHA256 bestätigt. Beide Scene-Berechtigungen erhalten; keine App-Daten gelöscht; nicht automatisch gestartet.
- PCM-/Gainmessung ist keine Hörabnahme: tatsächliche Wirkung/Lautstärke auf Quest-Lautsprechern/Kopfhörern, neue Griffgröße und globaler Mix mit Gegner-/Portalgeräuschen bleiben am getragenen Gerät zu prüfen. Keine neue Performance-/Thermikmessung. A6 (Gegnerklang) bleibt nächster offener Umsetzungsschritt.

## V18.6 – installiert: A4 Ashwarden-Revolver

- **0.18.6 / versionCode 24**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.6-revolver.apk`, **87926814 Bytes**.
- SHA-256: `a3687244e960b7dd3409ebc68b84cfd8a1a288b92bb51724b083385b8c8960a3`. AAPT bestätigt Version/min29/target36/ARM64; APK-v2-Signatur verifiziert, ein Signer.
- **401 native Prüfungen bestanden**: 363 vorherige, zwei zusätzliche Warmup-Assets, 36 Revolverprüfungen einschließlich produktivem Fire-/Nachladepfad und GPU-Vorschauen. Unity-Validierung/Export und Buildskript Exitcode 0. Log: `Verification/RevolverPolish/unity-export.log`. Bekannte SDK/TypeDB-Warnungen und zwei persistente Editor-Allokationen bleiben; keine Warnungsfreiheit behauptet.
- Originaler Blender-Revolver, gebackene Patina, bewegliche Trommel/Kran/Hahn/Abzug, sechs Patronen, ausschwenkendes Nachladen und auslaufender Laufrauch. 22.264 importierte Dreiecke, fünf Meshgruppen; bis 64 Partikel in drei wiederverwendeten Systemen. [Umfang, Vorschauen und offene Gates](REVIEW-V18-6.md).
- Export `/private/tmp/qdmr-v186-export.xscGnX`; Paketierung im bekannten temporären Arbeitsordner `/private/tmp/qdmr-v184-package.SE7n42`: **BUILD SUCCESSFUL in 1m 12s**, 117 Tasks, 29 ausgeführt, 88 unverändert. Log: `Verification/RevolverPolish/build-package.log`.
- Neue `libil2cpp.so` SHA-256 `a0a1664986974ef161c7f1b68d57a59791fcce02e43b2e30c547993007e9145b`. APK-Metadaten enthalten `RevolverMechanism`, `RevolverVfx`, `AshwardenRevolverV18Visual`, `AshwardenMuzzleFlame`, `LingeringBarrelSmoke`. V18.5 und V18.4 unverändert per ursprünglichem Hash bestätigt.
- **Installiert:** `adb install -r` Success auf Quest 3 `2G0YC5ZG9609PY`; `lastUpdateTime=2026-09-05 21:47:31`. Version 0.18.6 / Code 24 und installierte base.apk per obigem SHA256 bestätigt. Beide Scene-Berechtigungen erhalten; keine App-Daten gelöscht; nicht automatisch gestartet.
- Sicht-/Griff-/Stereo-/Live-Depth-Abnahme auf der Quest und neue Performance-/Thermikmessungen offen. Audio noch bisheriger Übergangsstand, **A5 ausdrücklich auf Revolver umgeplant**.

## V18.5 – installiert: A3 Feuer/Plasma

- **0.18.5 / versionCode 23**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.5-projectiles.apk`, **86173052 Bytes**.
- SHA-256: `752c8b076e166fbb57845e417b2b76e1ec6ae3ff25567dda31393133ab614e79`. AAPT bestätigt Paket/Version/min29/target36/ARM64; APK-v2-Signatur verifiziert.
- **363 native Prüfungen bestanden**: bisher 301, zwei zusätzliche Warmup-Assets und 60 A3-Prüfungen. Finale Unity-Validierung und Export erfolgreich, Exitcode 0. Log: `Verification/ProjectilePolish/unity-export.log`. Kein C#-/Shaderfehler im finalen Durchlauf; bekannte SDK/TypeDB-Warnungen und zwei persistente Editor-Allokationen weiterhin vorhanden.
- Blender 5.2: zwei eigene animierte Volumen-/Entladungsatlanten, in Unity ASTC 4×4, 16 interpolierte Frames. Handladung, Flug, auslaufende Spur und gerichteter Einschlag integriert; Collider bleiben abschießbar. Kollisionsreihenfolge prüft Raum vor einem dahinterliegenden Spieler. [Details und Grenzen](REVIEW-V18-5.md).
- Unity-Export `/private/tmp/qdmr-v185-export.u3LU0n`; inkrementelle Paketierung im bekannten temporären Gradle-Arbeitsordner `/private/tmp/qdmr-v184-package.SE7n42`. **BUILD SUCCESSFUL in 1m 4s**, 117 Tasks, davon 29 ausgeführt und 88 unverändert. Nur generierte Build-Eingaben dort ersetzt; vorheriger Unity-Export und V18.4-APK bleiben als Rückfall erhalten. Log `Verification/ProjectilePolish/build-package.log`.
- Neue `libil2cpp.so` SHA-256 `a7c6899e4e5db6be124fa810ad41f1fa682ac64830839760cf53c759eb9d64f2`; Paket-Metadaten enthalten die V18.5-Projektilpfade und Diagnosen. V18.4-APK per ursprünglichem SHA256 unverändert bestätigt.
- **Installiert:** Nach Anschluss der Quest 3 `2G0YC5ZG9609PY` meldet `adb install -r` Success; `lastUpdateTime=2026-09-05 21:22:19`, Version 0.18.5 / Code 23. SHA-256 der installierten base.apk stimmt mit obiger APK überein. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht, nicht automatisch gestartet.
- Unity-GPU-Vorschauen für Flug, Einschlag und vollständige Verdeckung durch Vordergrundgeometrie geprüft. Kein getragener Quest-Test und keine neue Performance-/Thermikmessung. Stereo, echte Live-Tiefenverdeckung und Lesbarkeit vor hellem Passthrough müssen auf der Quest beurteilt werden.

## V18.4 – vorherige Lieferung: Blender-Angriffsanimationen A1 + A2

Nachtrag: Der Nutzer bestätigt „Erwartungen soweit erfüllt“ und gibt A3 frei. Das bestätigt die subjektive Wirkung von A1/A2, nicht sämtliche offenen Hardware-/Performancegates.

- **0.18.4 / versionCode 22**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.4-combat-animation.apk`, **85153534 Bytes**.
- SHA-256: `4564375011a0710b12ec4b5d11f2437b9266b9138857e03ccc6096d8ffd7abc8`. AAPT bestätigt Version/ABI/min29/target36; APK-v2-Signatur verifiziert.
- Blender 5.2.0 LTS: Gesicht-/Schädelgewichte repariert, kompakter Nahkampf und Wurfgeste, echtes Bauch-/Beckengelenk plus U-Klauenangriff der Fledermaus. Originale Blender-Quellen und bisherige Produktions-FBXs als Rückfallstand erhalten. [Umsetzung, Bilder und Grenzen](REVIEW-V18-4.md).
- **301 native Unity-Prüfungen bestanden** (269 bisherige + 32 neue). Finaler Unity-Export erfolgreich, Exitcode 0: `work/unity-combat-v18.4-final.log` im übergeordneten Workspace. Kein neuer C#-/Shaderfehler oder Guard-Abbruch im finalen Export. Bekannte TypeDB-Warnungen und zwei persistente Editor-Allokationen weiterhin vorhanden; keine warnungsfreie Abnahme behauptet.
- Expliziter Gradle-Export `/private/tmp/qdmr-v184-export.U4SxcD`, finale Paketierung `/private/tmp/qdmr-v184-package.SE7n42`. Vollständige native IL2CPP-Kompilierung und `assembleRelease`: **BUILD SUCCESSFUL in 10m 8s**, 117 Tasks ausgeführt, Exitcode 0. Log `Verification/CombatPolish/gradle-package-final.log`.
- Erster Paketversuch ließ durch einen zu breiten Kopierfilter den exportierten IL2CPP-Compilerordner aus. Filter korrigiert; der finale Build enthält und verwendet den echten Compiler, keine Ersatzbibliothek oder ausgelassene Kompilierung. Ein alter Export-Guard wies beim ersten Unity-Versuch das neue temporäre Ziel zurück; im finalen Unity-Durchlauf korrigiert und neu geprüft.
- `adb install -r`: **Success**, `lastUpdateTime=2026-09-05 20:15:38`; Quest meldet 0.18.4/code22. Installierte `base.apk` per SHA-256 identisch zum Lieferpaket. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht. Spiel nicht automatisch gestartet. V18.3 per ursprünglichem SHA-256 unverändert bestätigt.
- Neue Bewegungen noch nicht am getragenen Headset abgenommen. Kein neuer Gameplay-/Performance-/Thermik-Erfolg behauptet. A3–A7 (Projektil-/Waffen-VFX, Klang und weitere Bewegungsdynamik) bleiben geplant im [vorgezogenen Zusatzplan](IMPLEMENTATION-COMBAT-POLISH.md).

## V18.3 – vorherige Lieferung: Spawn-/Wellenkorrektur

Nachtrag: Der Nutzer bestätigt V18.3 inzwischen als wesentlich besser. Das ist eine Nutzer-Rückmeldung, kein vollständiger technischer Raum-/Langzeittest.

- **0.18.3 / versionCode 21**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.3-spawn-fix.apk`, **84820290 Bytes**.
- SHA-256: `b6d27da33d8bcb0553e6c79cf4da8e70d47448d532e53035b105c3a16114650a`. AAPT bestätigt Version, ABI, min29/target36; APK-v2-Signatur verifiziert.
- **269 native Unity-Prüfungen bestanden** (252 bisherige + 17 neue), finaler Spielcode durch IL2CPP übersetzt. Unity-Log `work/unity-v18.3-spawn-delivery.log` im übergeordneten Workspace. Kein vollständiger erfolgreicher Unity-Paketierungsabschluss behauptet: Gradle im generierten Projekt hing, dieser Prozess wurde beendet.
- Nummerierte Konfliktdateien im generierten Daten-/Ressourcenordner beobachtet. 590 Hashdatei-Kopien mit vorhandenem Original wiederherstellbar quarantänisiert; danach finale kanonische Gradle-Kopie unter `/private/tmp/qdmr-v183-gradle.pWfKG7`. Dort `assembleRelease --no-daemon --no-parallel --max-workers=2 --offline`: **BUILD SUCCESSFUL in 2m 21s**, 116 Tasks ausgeführt, Exitcode 0. Log: `Verification/V18/v183-staged-gradle.log`. Keine nummerierten Zusatzdateien im fertigen APK-Inventar gefunden.
- Staging-`libil2cpp.so` per SHA256 identisch zum finalen Unity-Player (`df66ce4d1fdd017e9a62cd1ae7c77bbfba563d34f7d91173165517ce8dfa0418`). APK-Metadaten enthalten die neue Wartelogik, Randproben-Ablehnungsgründe und Wiederverwendungsdiagnostik.
- `adb install -r`: Success, `lastUpdateTime=2026-09-05 19:27:11`; installierte base.apk per SHA256 identisch. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht. V18.2 und V18.1 unverändert per Hash bestätigt.
- Nicht automatisch gestartet. Logaufnahme für den nächsten Nutzerstart vorbereitet. Tatsächliche Gegnerspawns im Nutzerraum noch nicht bestätigt; die Bodennaht als Fehlerfall wurde im Kollisions-Test reproduziert, nicht als alleiniger Gerätefehler bewiesen.

Details und Wiederherstellbarkeit der Quarantäne: [V18.3-Bericht](REVIEW-V18-3.md).

## V18.2 – vorherige Lieferung: kompakte Blender-Portale, Spawnregression gemeldet

- **0.18.2 / versionCode 20**, ARM64/IL2CPP/Vulkan, nicht Development/Debuggable. APK `Builds/QuestDemonMR-v18.2-portals.apk`, **84819324 Bytes**.
- SHA-256: `8e9ae0807665991c260eb2fa32a6d05192a900682e1357628b996f2dab8da3b1`. AAPT bestätigt Version/ABI/min29/target36, APK-v2-Signatur verifiziert.
- **252 native Prüfungen bestanden**, Unity Exitcode 0, Build erfolgreich. Log: `work/unity-v18.2-portals.log` im übergeordneten Workspace. Neues FBX und beide gebackenen Texturen im Buildbericht enthalten, Randshader für Vulkan serialisiert. Bekannte TypeDB-Warnungen und zwei nicht zugeordnete persistente Editor-Allokationen weiterhin vorhanden; keine warning-/leakfreie Abnahme.
- Blender 5.2 erzeugte Quellmodell, FBX, Albedo/Normalmaps und eine Vorschau; 22.460 Dreiecke in drei Meshes. Zusätzliche Unity-Vorschau mit Produktionsmaterialien visuell geprüft.
- `adb install -r`: Success auf Quest 3, `lastUpdateTime=2026-09-05 18:34:38`. Installierte base.apk per SHA-256 identisch, Scene-Berechtigungen erhalten, keine App-Daten gelöscht. V18.1 und V18.0 per Hash unverändert bestätigt.
- Nicht automatisch gestartet. Größenwirkung, Austritt an Engstellen und vollständige Portaloptik müssen im Headset beurteilt werden; kein neuer Gameplay-/Performance-Erfolg behauptet. V18.1 wurde vom Nutzer inzwischen als funktionierend bestätigt; die Laser-Tag-Abweichung bleibt offen und die Scan-Architektur unverändert.

Umfang, Maßtabelle, Prüfgrenzen und Reproduktion: [V18.2-Bericht](REVIEW-V18-2.md), `bash Tools/build-v18.2.sh`.

## V18.1 – vorherige Lieferung: Start-Hotfix

- **0.18.1 / versionCode 19**, ARM64/IL2CPP/Vulkan, kein Development/Debuggable. APK `Builds/QuestDemonMR-v18.1-livescan.apk`, **83854552 Bytes**.
- SHA-256: `d61b3e7813fc20239629e6c620226bdf4cb1f1ac563ef10f13753f28a26f679b`. AAPT bestätigt Version, ABI und SDK 29/36; APK-v2-Signatur geprüft.
- **229 native Prüfungen bestanden** (220 bisherige + 9 Start-/Shaderregressionen). Unity Exitcode 0, Build erfolgreich: `work/unity-v18.1-hotfix.log` im übergeordneten Workspace. WeaponUnlit für Vulkan kompiliert und serialisiert. Bestehende TypeDB-Warnungen und zwei nicht zugeordnete persistente Editor-Allokationen weiterhin vorhanden.
- `adb install -r`: Success, `lastUpdateTime=2026-09-05 18:04:40`, beide Scene-Berechtigungen weiterhin erteilt. SHA-256 der installierten base.apk identisch. Keine App-Daten gelöscht. V18.0 und V17.2 per Hash unverändert bestätigt.
- Echter Quest-3-Start um **18:06:01**: `QDMR_SCAN_CREATED`, `QDMR_GUN_READY weapon_shader=QuestDemonMR/WeaponUnlit`, `QDMR_LIVE_DEPTH ready=True`, alle neun Inhalte ohne fehlende Assets in 0,463 s vorbereitet. Nach zehn Sekunden Sensor aktiv, 121 Feldabschnitte, 97 Oberflächenabschnitte, Revision 636; noch keine Boden-/Spielstartfreigabe an diesem Messpunkt. Damit ist der ursprüngliche Startabbruch behoben, nicht bereits die komplette Raum-/Gameplayqualität abgenommen.

Weitere reale Erfassung bis 18:06:41: 128 Feldabschnitte, 93 Oberflächenabschnitte, Boden erkannt (`floorY=0,039`), Revision 2365, noch `ready=False`. Anschließend USB/ADB getrennt, daher kein späterer Status bestätigt. Auszug: `Verification/V18/hotfix-0181-device.txt`.

Ursache und Änderungen: [V18.1-Bericht](REVIEW-V18-1.md). Reproduktion: `bash Tools/build-v18.1.sh`.

## V18.0 – vorherige Lieferung, Startfehler auf dem Gerät

- Version **0.18.0**, versionCode **18**, ARM64/IL2CPP/Vulkan, kein Development/Debuggable. Unity 6000.3.2f1.
- APK: `Builds/QuestDemonMR-v18-livescan.apk`, **83853114 Bytes**. Unitys größere BuildReport-Gesamtsumme ist nicht die APK-Dateigröße.
- SHA-256: `e11415b209b979031ad6b1a98358b290534953841fe30ad6f09b80faa2130808`.
- `aapt` bestätigt Paket `de.stefanmaier.questdemonmr`, Version/Code, ARM64, minSdk 29, targetSdk 36 und fehlendes Debuggable. `apksigner verify --verbose`: v2 erfolgreich, ein Signer.
- **220 native Unity-Prüfungen** bestanden: 179 bisherige und 41 Live-Scan-Prüfungen einschließlich echter Compute-Ausführung mit synthetischen Tiefendaten auf dem Mac-Grafikgerät. Kein Ersatz für Quest-Sensor-/Raumqualität.
- Abschließender Build erfolgreich, Unity Exitcode 0; `QDMR_SCAN_VALIDATION_OK checks=41`, `Build Finished, Result: Success`, `QDMR_BUILD_OK` in `work/unity-v18-livescan-final.log` des übergeordneten Workspace.
- Erster Anlauf wegen eines Vorschau-Shaderfehlers vor Lieferung gestoppt. Fehler und knappe Bodenfreiheit korrigiert; zweiter Build erfolgreich, danach letzte Startpult-/Pausensynchronisationskorrekturen im finalen Build. Nur finalen Stand installiert.
- `adb install -r`: **Success** auf Quest 3 `2G0YC5ZG9609PY`. `lastUpdateTime=2026-09-05 17:33:12`; beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht. SHA-256 der installierten `base.apk` entspricht obiger APK.
- V18-Typen und finale Startpult-Methode in den verpackten IL2CPP-Metadaten zusätzlich bestätigt. Nicht automatisch gestartet; andere laufende Quest-Anwendungen nicht unterbrochen. Kein realer V18-Scan-/Gameplay-Lauf behauptet.
- V17.2-Rückfall-APK unverändert erhalten und Hash erneut geprüft: `Builds/QuestDemonMR-measure-v17-startup.apk`.

Bedienung, Funktionsumfang, Speicher-/Lifecycle-Grenzen und Warnungen: [REVIEW-V18.md](REVIEW-V18.md). Reproduktion: `bash Tools/build-v18.sh`. Die frühere unversionierte V17-Mess-APK bleibt V17.2; V18 hat bewusst einen eigenen Dateinamen.

## V17.2 – erhaltener Vergleichsstand

Nicht-Development-Messbuild `0.17.2` / versionCode `17`, ARM64/IL2CPP/Vulkan. Asynchrones Vorladen der Spawnassets vor dem Startpult, wiederverwendete Animationssammlungen und Sound-Vorbereitung; Benchmark-Gegneraufbau über mehrere Frames verteilt. Details: [REVIEW-V17-2.md](REVIEW-V17-2.md).

- APK: `Builds/QuestDemonMR-measure-v17-startup.apk`, identisch mit `Builds/QuestDemonMR-measure-v17.apk`.
- Größe: **83841480 Bytes**.
- SHA-256: `5cd37726e1027a90ff9aee175e1853c059221205cfcb8cfeba0ff9f3c8a0830c`.
- `aapt`: 0.17.2 / 17, ARM64, kein Debuggable; `apksigner verify --verbose`: v2 erfolgreich, ein lokaler Signer.
- **179 native Unity-Prüfungen** bestanden (160 bestehende + 19 Startup-Prüfungen) in `work/unity-v17-startup-build.log`. Außerdem aktueller Offline-Compile, 36 reine C#-Checks und fünf Python-Tests erfolgreich; diese Kernprüfungen nicht zusätzlich zur Gesamtzahl addieren.
- Der erste Gradle-Paketierungsversuch scheiterte an zwei nummerierten Konfliktkopien. Sie sind unter `Verification/GradleQuarantine/20260905-125719-c66ec1d1/` gesichert, nicht gelöscht. Ein zunächst zu strenger Pfadschutz wurde korrigiert. Details und Logs im V17.2-Bericht.
- Finaler Unity-/Android-Build **erfolgreich**, Exitcode 0, `Build Finished, Result: Success` / `QDMR_BUILD_OK` in `work/unity-v17-startup-rebuild2.log`.
- `adb install -r`: `Success`, `lastUpdateTime=2026-09-05 14:59:22`. Installierte `base.apk` per SHA-256 identisch geprüft. Beide Scene-Berechtigungen erhalten, keine App-Daten gelöscht.
- Der erste Start wurde vom Controller-Systemdialog aufgehalten. Später absolvierte der Nutzer den vollständigen V17.2-Test: Sitzung `v17-20260905-142615-2042f282`, 60,011 Sekunden, zwei Portale und zwei statt drei sicher platzierbare Gegner. Erste Portalspitze 19,469 ms, Gegnerphase maximal 18,219 ms; Kampfmittel 13,886 ms. Geringere Gegnerlast begrenzt den Vergleich. Originaldaten unter `Verification/V17/Quest-Measurement-Startup/`; Details und fehlende Startup-/GPU-Messwerte im [V17.2-Bericht](REVIEW-V17-2.md). Keine vollständige V17-Abnahme.

Reproduktion der aktuellen Version einschließlich sämtlicher Spielprüfungen: `QuestDemonMR.Editor.StartupValidation.ValidateAndBuild`.

## V17.1 – erhaltener Vergleichsstand

Optimierter Nicht-Development-Messbuild `0.17.1` / versionCode `17`, Android ARM64/IL2CPP/Vulkan, Unity 6000.3.2f1. Exaktes Sparse-Skinning für Wunden und korrigierter GC-Zähler; keine Änderung von Waffenpose, Portalauflösung oder exakten Mesh-Schüssen.

- APK: `Builds/QuestDemonMR-measure-v17-optimized.apk`. Die unversionierte Mess-Ausgabe wird inzwischen von V17.2 verwendet.
- Größe: **83834794 Bytes**.
- SHA-256: `cffcc92ebf25cd310c0815d9a02cf18d9d5642ff3c4b46afc0fdbf6b267eae52`.
- `aapt`: Version 0.17.1 / Code 17, ARM64, kein `application-debuggable`; `apksigner`: verifiziert, v2, ein lokaler Signer.
- **160 native Unity-Regressionstests bestanden**, außerdem 36 reine C#-Checks im Hilfsskript und fünf Python-Auswertungstests. Die 36 sind Teil der 160, nicht zusätzlich 196 unabhängige Tests.
- `work/unity-v17-optimized-build.log`: `QDMR_V17_SPARSE_REGRESSION_OK`, `QDMR_V17_REGRESSION_OK`, `Build Finished, Result: Success`, `QDMR_BUILD_OK`; Unity Exitcode 0.
- `adb install -r`: `Success`, `lastUpdateTime=2026-09-05 14:35:31`; keine App-Daten gelöscht. Beide Scene-Berechtigungen bleiben erteilt.
- Installierte `base.apk` anhand SHA-256 identisch geprüft.
- Direkter erfolgreicher Kaltstart der tatsächlichen `UnityPlayerGameActivity`; Spielprozess **15326**, danach Raum mit 41 Ankern/9 Wänden geladen. Diesmal kein Controller-Dialog.
- Controller-gestartete Messung `v17-20260905-123555-93fc2a8c` vollständig nach 60,003 Sekunden beendet und gesichert: zwei sichtbare Portale, drei Gegner. Vollhaut-Bakes in der Kampfphase von 1705 auf 90 reduziert, exakte Dreieckstests unverändert; beide Messbuilds rund 72 Hz im Mittel. Ergebnisse und Grenzen im [Geräteprofil](V17-FIRST-DEVICE-PROFILE.md).

Die vorherigen Development- und Mess-Basis-APKs sind erhalten. Die unversionierte **Debug**-APK enthält weiterhin die Development-Basis 0.17.0, nicht die optimierte Nicht-Development-Variante. Zum Reproduzieren der aktuellen Lieferung `FollowupV17Validation.ValidateAndBuildMeasurement` verwenden.

## V17.0 – Vergleichsstände dieses Schritts

### Nicht-Development-Basis

- `Builds/QuestDemonMR-measure-v17-baseline.apk`, Version 0.17.0 / Code 17, **83734686 Bytes**.
- SHA-256 `a3dcff7467fd899d4bd5e36407c51d30330417c1fc91472f130d74a73ff108db`.
- `aapt` bestätigt ARM64/Version und fehlendes Debuggable; `apksigner` v2 erfolgreich. Unity-Build Exitcode 0; `work/unity-v17-measure-build.log`.
- Installation als Update erfolgreich, `lastUpdateTime=2026-09-05 14:22:50`; installierter Hash geprüft. Nach Controller-Dialog tatsächlicher Spielprozess 24650.
- Vollständiger 60-Sekunden-Lauf mit zwei sichtbaren Portalen und drei Gegnern, Daten gesichert. Kampf-Framezeit im Mittel 13,889 ms; detaillierte Grenzen und kurzer zusätzlicher VrApi-Ausschnitt im [Geräteprofil](V17-FIRST-DEVICE-PROFILE.md).

### Development-Basis

V17 Development ist mit Unity 6000.3.2f1 für Android ARM64/IL2CPP gebaut, signaturgeprüft und als Update auf Quest 3 `2G0YC5ZG9609PY` installiert. Keine App-Daten gelöscht. Die frühere Systemblockade besteht nicht mehr.

- APK: `Builds/QuestDemonMR-debug-v17.apk`; Standard-Ausgabe `Builds/QuestDemonMR-debug.apk`.
- Paket: `de.stefanmaier.questdemonmr`; Version `0.17.0`, versionCode `17`; minSdk 29, targetSdk 36.
- APK-Größe: **141404839 Bytes**.
- SHA-256: `80ad23863f9ba1d84575ef37c8399140a9e44c3a09e1583f0f95d40e1120a48e`.
- `aapt`: ARM64, Version und Development/Debuggable bestätigt.
- `apksigner verify --verbose`: erfolgreich, Signature Scheme v2, ein lokaler Signer.
- **103 native Unity-Editor-Assertions bestanden:** 28 V15 + 39 V16 + 36 V17. Zusätzlich fünf Python-Auswertungstests bestanden.
- `Build Finished, Result: Success` und `QDMR_BUILD_OK` in `work/unity-v17-build.log` des übergeordneten Workspace; Unity Exitcode 0.
- `adb install -r`: `Success`; PackageManager `lastUpdateTime=2026-09-05 14:03:27`.
- Beide Scene-Berechtigungen weiterhin erteilt. SHA-256 der installierten `base.apk` stimmt mit obiger lokaler APK überein.
- Erster Start wurde vom Controller-Systemdialog aufgehalten; unmittelbar danach kein Spielprozess. Getragener Start, Benchmark und Leistungswerte sind dadurch noch nicht nachgewiesen.

Auch dieser Development-Build wurde später tatsächlich gestartet und absolvierte einen vollständigen Messlauf (ein sicher platzierbares Portal, zwei Gegner). Rohdaten sind im Geräteprofil verlinkt. Beide Buildvarianten verwenden dasselbe Paket, ersetzen einander also auf dem Gerät.

V17-Funktionen, Bedienelemente und offene Geräte-/Performance-Gates: [REVIEW-V17.md](REVIEW-V17.md). Kein behaupteter FPS-Gewinn und keine vollständige V17-Abnahme aus dem Build abgeleitet. Der bekannte unbenutzte Meta-URP-Beispielshader-Hinweis (`DOTS.hlsl`) sowie TypeDB-Doppelregistrierungen erscheinen weiterhin; der Build ist erfolgreich, aber nicht warnungsfrei.

## Historischer V16-Nachweis

Die folgenden Abschnitte dokumentieren den damaligen V16-Build/Installationslauf. V16 wurde danach vom Nutzer als abgeschlossen bestätigt. Die versionierte V16-APK bleibt unverändert erhalten; die unversionierte Standard-Ausgabe wird inzwischen von V17 verwendet.

## Ergebnis

V16 wurde mit Unity 6000.3.2f1 als Android-Development-Build (IL2CPP, Vulkan, ARM64) gebaut, signaturgeprüft und als Update auf der bekannten Quest 3 installiert. Spielstart und getragenes Headset-QA sind noch offen: Horizon OS fordert beim Start aktive Controller an.

- APK: `Builds/QuestDemonMR-debug-v16.apk`; identische Kopie unter `Builds/QuestDemonMR-debug.apk`
- Paket: `de.stefanmaier.questdemonmr`
- Version: `0.16.0`, versionCode `16`
- Android: minSdk 29, targetSdk 36
- APK-Größe: 141281599 Bytes (Dateigröße, nicht Unitys summierte Build-Artefaktgröße)
- SHA-256: `059a9738ee1cf50ae8d0710212d8c3810ea312d51ff18a2f6a196e99c114eed6`

Änderungen, Ursachen, Quellen und Testplan: [REVIEW-V16.md](REVIEW-V16.md).

## Tatsächlich ausgeführte Prüfungen

- Vollständiger C#-/Android-Player-Build erfolgreich: `Build Finished, Result: Success`, `QDMR_BUILD_OK`.
- 67 Editor-Assertions erfolgreich: 28 aus V15 plus 39 aus V16. Beide Marker `QDMR_V15_REGRESSION_OK` / `QDMR_V16_REGRESSION_OK` im finalen Buildlog. Produktions-Hitgeometrie, Skinning/Wunden, Pause, leere Konsole, Möbel-/Bodenlandung, langer Fall, Off-Axis-Projektion/Stereo-Parallaxe, Sounddaten, Modellimporte und Controlleroffset geprüft.
- Blender 5.2 LTS tatsächlich ausgeführt: neue Steinschwelle/Ketten exportiert; 6490 umgekehrt orientierte Weltflächen repariert und ihre Normalen im Blender-Skript geprüft. Alte Quelldateien bleiben erhalten.
- Zehn Unity/Metal-Bilder erzeugt (`Previews/V16/`); Portal-Einblicke einschließlich Nahansicht und Einzelaugentextur visuell kontrolliert. Danach hinzugefügte Frontseiten-/Deaktivierungslogik ist im finalen Build enthalten. Keine Quest-MR-Aufnahmen.
- Zwanzig Mono-PCM-Audios geprüft: geladen, Daten lesbar, nicht stumm und keine Einzelclip-Übersteuerung; unmittelbare Schusswiederholungen ausgeschlossen. Kein Hörtest über Quest-Lautsprecher behauptet.
- `aapt`: Version 16 / 0.16.0, SDK-Werte und `arm64-v8a` bestätigt.
- `apksigner verify --verbose`: verifiziert, Signature Scheme v2, ein Debug-Signer.
- `adb install -r`: `Success`; keine App-Daten gelöscht.
- PackageManager: Version 16 / 0.16.0, `lastUpdateTime=2026-09-05 09:29:01`.
- Beide Scene-Berechtigungen (`com.oculus.permission.USE_SCENE`, `horizonos.permission.USE_SCENE`) weiterhin erteilt.
- SHA-256 der installierten `base.apk` stimmt mit der lokalen, versionierten APK überein.

Logs im übergeordneten Workspace `work/`: `unity-v16-build.log` (final), `unity-v16-world-final.log` (finale Bilder), `unity-v16-visual2.log` (erste Prüfungen). Der erste Importversuch benötigte eine Anpassung an Unity 6.3s neue Audio-Preload-Einstellung; der finale Build enthält diese Korrektur und keine C#-Kompilierfehler.

## Hardwarestatus und Startdialog

Ziel: Quest 3 `2G0YC5ZG9609PY`.

Der ADB-Startaufruf meldet zwar `Status: ok`, tatsächlich gestartet wird aber:

`com.oculus.vrshell/.systemdialog.launchcheck.LaunchCheckControllerRequiredDialogActivity`

Beim anschließenden Check liefert `pidof de.stefanmaier.questdemonmr` keinen Spielprozess. Deshalb ist dies kein erfolgreicher Laufzeit-Smoke-Test. Controller aufwecken, den Systemdialog im Headset abschließen und die installierte App öffnen. Die Systemanforderung wurde nicht umgangen.

Die neue Portalauflösung, Augenmatrizen und Vulkan-UV-Ausrichtung müssen am getragenen Headset geprüft werden. Ebenso Fledermaus-Ausweichen bei zeitweise unbekannter Tiefe, Aufprall auf Sofa/Boden, Waffenpose, Soundmischung und eine längere Performance-/Thermiksitzung. Keine stabile Bildrate allein aus Build oder Editor behauptet.

## Bekannte Warnungen

- Der mitgelieferte, im Built-in-Render-Pipeline-Spiel nicht verwendete Meta-URP-Beispielshader `Meta/MRUK/MixedReality/MRUKLit` meldet weiterhin ein fehlendes `DOTS.hlsl`. Derselbe Hinweis steht bereits in früheren Buildlogs. Der Player-Build ist dennoch erfolgreich; keine Shader-Kompilierfehler der V16-Spielshader gefunden.
- Der Build schreibt umfangreiche TypeDB-Doppelregistrierungshinweise von Unity-/SDK-/Editor-Bibliotheken. Sie verhindern diesen Build nicht.
- Desktop-/Metal-Renderings ersetzen keine Prüfung von Vulkan, Stereo und realer Environment Depth auf Quest.

## Reproduktion

- Tests und Build: `QuestDemonMR.Editor.FollowupV16Validation.ValidateAndBuild`
- Import, Tests und Bilder: `QuestDemonMR.Editor.VisualReviewCapture.PrepareAndCapture`
- Nur Bilder: `QuestDemonMR.Editor.VisualReviewCapture.Capture`
- Blender: `BlenderSource/build_portal_threshold_v16.py` und `BlenderSource/repair_world_v16.py`.
- Audio: `ExternalSource/KenneyAudioV16/mix_combat.py` (ffmpeg erforderlich).

## Vorversion erhalten

Die zuvor getestete V15 liegt weiterhin in `Builds/QuestDemonMR-debug-v15.apk`, SHA-256 `6570928acbf81df7b31b7cc2cc44da94bceac3e5b98f3bcebf7a595ea10546c3`. Ihr Änderungsbericht bleibt in `Docs/REVIEW-V15.md`. Ein Downgrade wurde nicht durchgeführt.
