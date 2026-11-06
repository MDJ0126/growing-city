# Prototype GrowingCity

[English](../en/README.md) · [한국어](../ko/README.md) · [日本語](../ja/README.md) · [简体中文](../zh-Hans/README.md) · [繁體中文](../zh-Hant/README.md) · [Deutsch](../de/README.md) · [Français](../fr/README.md) · [Español](../es/README.md) · [Português](../pt/README.md)

Bombardez une ville qui grandit seule. Les bâtiments voisins évoluent ensemble et les zones détruites se reconstruisent.

Ouvrez Assets/Scenes/City Prototype.unity avec Unity 6000.0.83f1 et URP 17.0.4. Le projet est un prototype Unity ; son exécution dans un navigateur reste non vérifiée.

Tracez une direction sur la ville puis relâchez pour lancer la formation. Les avions traversent tout l’écran depuis l’extérieur. 1 : bombe, 2 : nucléaire, Espace : pause/reprise, Échap : annuler.

Chaque avion commence à une bombe par seconde. Améliorez cadence, capacité, rayon, puissance, formation, vitesse, blindage et résistance pour les prochains vols. La croissance urbaine ne s’achète plus.

La croissance accélère avec le temps de jeu : 1x au départ, 2x à 60 secondes, jusqu’à 8x à 420 secondes, affiché sur le HUD. Seuls les bâtiments de niveau 3 ou plus tirent des missiles. Chaque douzième construction est un dépôt de carburant jaune, un dépôt de munitions rouge ou une centrale cyan. Leur destruction inflige des dégâts alentour et peut déclencher une chaîne. L’or est versé une seule fois pour la chaîne ; le développement baisse et les ruines restent reconstructibles.

Chaque avion arrivé ajoute exactement 50% de recharge normale. Deux arrivées donnent une utilisation, les surplus sont conservés. La recharge passive prend 10 secondes de jeu et remplit au maximum une utilisation.

Le nucléaire a un délai indépendant de 100 secondes dès acceptation d’un ordre valide, y compris en file. Disponible au départ, sans consommer de charge normale. Les arrivées et la vitesse 3x ne le réduisent pas. La pause arrête le compteur ; Nouvelle ville le réinitialise.

Les bâtiments tirent des missiles de niveau adapté au suivi limité. Les avions peuvent être endommagés et abattus. Les bâtiments évolués ont davantage de résistance et peuvent rétrécir. La destruction rapporte de l’or et réduit le développement urbain.

Au premier lancement, la langue suit le système. Le bouton de langue parcourt Système et neuf langues ; le choix est enregistré sur cet appareil. Une langue non prise en charge utilise l’anglais.

## Commandes et paramètres

Les originaux externes Assets/polyperfect/Low Poly Epic City/ restent intacts. Utilisez des ressources propres au projet et des adaptateurs. Ne redistribuez pas séparément les ressources du Store.

Limites : affichage Unity, changement de langue en jeu, déploiement Web et équilibrage non vérifiés ici. La compilation et les tests indépendants ne remplacent pas un test Play.

[HTML](index.html)

Les bâtiments explosifs sont peints en bleu vif ; les fondations jaunes, rouges ou cyan indiquent leur type. La destruction combine basculement latéral, torsion et soulèvement explosif. Les sections de façade et petits débris suivent la physique, parfois au-dessus des toits ou hors écran. Au maximum 36 sections partagent le budget existant. Les nouveaux effets et le shader de fragments restent à vérifier dans Play.

La ville commence avec96 cases occupées, des combinaisons et des bâtiments spéciaux. La simulation indépendante standard donne82 bâtiments. Chaque cycle ajoute jusqu’à4 cases sur des bords connectés et séparés, par tirage pondéré plutôt que par distance au centre. La limite est24×24, soit576 cases (environ384×384unités Unity pour une case de16). Une carte pleine cesse de s’étendre mais reste reconstructible. De90 à300 secondes de jeu, les niveaux élevés directs deviennent plus fréquents. Dès300 secondes, les bâtiments ordinaires nouveaux ou reconstruits ont75% de niveau3,20% de niveau2 et5% de base. Les spéciaux gardent leurs règles. Nouvelle ville réinitialise la progression.
