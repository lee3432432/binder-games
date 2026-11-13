#!/usr/bin/env python3
# 캐릭터 설정(JSON)으로 DeskTyper.cs 템플릿을 바꿔서 exe 를 빌드
import json, re, subprocess, sys, shutil, os
cfg=json.load(open(sys.argv[1])); src=open(os.path.join(os.path.dirname(__file__),'DeskTyper.cs'),encoding='utf-8').read()
def pts(q): return ', '.join(f'new PointF({x}, {y})' for x,y in q)
def pf(p): return f'new PointF({p[0]}f, {p[1]}f)'
s=src
s=re.sub(r'static readonly PointF\[\] PAD = \{.*?\};', f'static readonly PointF[] PAD = {{ {pts(cfg["PAD"])} }};', s)
s=re.sub(r'static readonly PointF\[\] KB = \{.*?\};', f'static readonly PointF[] KB = {{ {pts(cfg["KB"])} }};', s)
s=re.sub(r'static readonly PointF SH_MOUSE = .*?;\n', f'static readonly PointF SH_MOUSE = new PointF({cfg["SH_MOUSE"][0]}, {cfg["SH_MOUSE"][1]}), SH_KEY = new PointF({cfg["SH_KEY"][0]}, {cfg["SH_KEY"][1]}), UP_SLEEVE = new PointF({cfg["UP_SLEEVE"][0]}, {cfg["UP_SLEEVE"][1]});\n', s)
P=cfg['P']
s=re.sub(r'const float MOUSE_S = .*?;', f'const float MOUSE_S = {P["mouse"]["s"]}f, GRIP_S = {P["grip"]["s"]}f, POINT_S = {P["point"]["s"]}f, UP_S = {P["up"]["s"]}f;', s)
s=re.sub(r'static readonly PointF GRIP_PALM = .*?;\n', f'static readonly PointF GRIP_PALM = {pf(P["grip"]["palm"])}, GRIP_SLEEVE = {pf(P["grip"]["sleeve"])}, POINT_TIP = {pf(P["point"]["tip"])}, POINT_SLEEVE = {pf(P["point"]["sleeve"])}, UP_SLEEVE_A = {pf(P["up"]["sleeve"])};\n', s)
A=cfg['ARM']
s=s.replace('new Pen(Color.FromArgb(17, 17, 17), 40 * s)',f'new Pen(Color.FromArgb({A["line"]}), {A["w"]+10} * s)')
s=s.replace('new Pen(Color.FromArgb(251, 251, 255), 30 * s)',f'new Pen(Color.FromArgb({A["fill"]}), {A["w"]} * s)')
s=s.replace('new Pen(Color.FromArgb(140, 180, 170, 210), 8 * s)',f'new Pen(Color.FromArgb({A["shade"]}), 8 * s)')
s=s.replace('Color.FromArgb((int)(230 * a), 64, 224, 255)',f'Color.FromArgb((int)(230 * a), {cfg["HIT"]})')
s=s.replace('"DeskTyper", "counts.txt"',f'"{cfg["id"]}", "counts.txt"').replace('"DeskTyper", "settings.txt"',f'"{cfg["id"]}", "settings.txt"')
s=s.replace('"DeskTyper_Single"',f'"{cfg["id"]}_Single"').replace('타자치는 토끼',cfg['title']).replace('"Desk Typer"',f'"{cfg["product"]}"')
kt=json.loads(subprocess.run(['node',os.path.join(os.path.dirname(os.path.abspath(__file__)),'keytable.js'),sys.argv[1]],capture_output=True,text=True,check=True).stdout)
s=re.sub(r'static readonly Dictionary<uint, KeyPos> KEYS = new Dictionary<uint, KeyPos> \{.*?\n        \};', lambda m:'static readonly Dictionary<uint, KeyPos> KEYS = new Dictionary<uint, KeyPos> {\n            '+kt['keys']+'\n        };', s, flags=re.S)
s=s.replace('const float KW = (.715f - .035f) / 15f;', f'const float KW = {kt["kw"]:.5f}f;')
s=s.replace('v0 = k.v - .065f, v1 = k.v + .065f', f'v0 = k.v - {kt["kh"]}f, v1 = k.v + {kt["kh"]}f')
out=cfg['id']+'.cs'; open(out,'w',encoding='utf-8').write(s)
assets=cfg['assets']; res=[]
for n in ['scene','mouse','hand_grip','hand_point','hand_up']:
    shutil.copy(os.path.join(assets,n+'.png'),n+'.png'); res+=[f'-resource:{n}.png,{n}.png']
shutil.copy(cfg['icon'],'icon.ico')
cmd=['mcs','-target:winexe','-sdk:4.5','-optimize+','-codepage:utf8','-win32icon:icon.ico','-resource:icon.ico,icon.ico',*res,'-r:System.Windows.Forms.dll','-r:System.Drawing.dll',f'-out:{cfg["id"]}.exe',out]
r=subprocess.run(cmd,capture_output=True,text=True);print(r.stdout[-800:],r.stderr[-800:]);print('built',cfg['id']+'.exe')
