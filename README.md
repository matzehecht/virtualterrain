# Teilnehmer Gruppenarbeit
- Nicole Graf
- Tim Leschinsky
- Matthias Hecht
- Florian Glökler

# Unity Version
verwendete Unity Version bei der Entwicklung: 2019.1.3f1

# Szenen und Notizen
Das Repository beinhaltet zum Einen das gesamte Unity Projekt und zum Anderen einen 
direkten Windows Build (im Directory "Windows Build") der entwickelten Applikation.

- Windows Build:
Um unsere Unity Anwendung bestmöglich "erleben" zu können, kann direkt die exe-Datei "virtualterrain"
im "WindowsBuild"-Ordner gestartet werden. Dadurch lässt sich die Szene unabhängig vom Unity
Editor ausführen und erkunden. Ausgehend von einem Menü zur Einstellung der wichtigsten Skript- und
Shader-Parameter kann der Erkundungsspaß gestartet werden. Zusätzliche Informationen und Hilfestellungen 
rund um die Applikation können über den Menüpunkt "Help" eingesehen werden, welcher dementsprechend
vor dem ersten Start gerne geöffnet werden kann.

- Unity Projekt:
Zusätzlich zum Windows Build wollten wir auch noch die Funktionalität offen behalten, dass die
Szene innerhalb des Unity Editors eingesehen und darüber gestartet werden kann. Dadurch hat man
die Möglichkeit, noch weitere Parameter zur Generierung und Darstellung der Szene
einzustellen, die aus dem Einstiegsmenü des Windows Build heraus gelassen wurden, um den Anwender
nicht zu überfordern oder zu überladen. Nach Einstellung der weiteren Parameter im Unity Editor
kann dort dann entweder direkt die Szene "TerrainScene" gestartet werden oder wiederum die 
Szene "MainMenue", um ebenfalls das Menü als Einstiegspunkt zu haben, was dann aber nicht
mehr zwingend notwendig wäre.