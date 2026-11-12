# 타자치는 토끼 (Desk Typer) 윈도우 앱 빌드
이미지(scene/mouse/hand_*.png)는 games/shiba-typer/ 에 있음. 같은 폴더에 복사 후:
mcs -target:winexe -sdk:4.5 -optimize+ -codepage:utf8 -win32icon:icon.ico -resource:icon.ico,icon.ico \
  -resource:scene.png,scene.png -resource:mouse.png,mouse.png -resource:hand_grip.png,hand_grip.png \
  -resource:hand_point.png,hand_point.png -resource:hand_up.png,hand_up.png \
  -r:System.Windows.Forms.dll -r:System.Drawing.dll -out:DeskTyper.exe DeskTyper.cs
