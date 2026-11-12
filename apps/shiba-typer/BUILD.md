# 시바 타자왕 윈도우 앱 빌드
프레임 이미지(f0~f5.png)는 games/shiba-typer/ 에 있음.
mcs -target:winexe -sdk:4.5 -optimize+ -codepage:utf8 -win32icon:icon.ico -resource:icon.ico,icon.ico \
  -resource:f0.png,f0.png ... -resource:f5.png,f5.png -r:System.Windows.Forms.dll -r:System.Drawing.dll -out:ShibaTyper.exe ShibaTyper.cs
