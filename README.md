#**RobotArm - Drive a robot arm whith Kinect**
##Code *Xaml* & *C#* pour contrôler le bras robotique avec un Kinect (xbox 360) et bracelet Myo.

######Kinect
Le Kinect est relié au PC. Les données récupérés pour le contrôle du bras sont les articulations.
Les articulations permettent le calcul des angles réalisé avec le bras. Le Kinect ne pouvant pas détecter les mouvements de la main, nous utilisons un Myo.

######Myo
Le Myo permet de détecter les mouvements de la main. Le logiciel utilisé est celui fourni avec, permettant de mapper un mouvement sur une touche de clavier.

######SSC-32U
Le bras robotique est contrôlé par une carte électonique faites pour le contrôle de servomoteurs. Cette carte est la *SSC-32U*.
La communication entre la carte et le PC est série via USB. Plus précisemment :
- Baud Rate : 9600 (possibilité de le modifier)
- Data bits : 8
- Hand Shaking : None
- Parité : None
- Bits de stop : 1


Ce projet a été réalisé pour le _Mini Maker Faire 2017_ de Perpignan.

:shipit: Par _Florentin DION_ et _Guillaume DUPLOUICH_ :shipit: