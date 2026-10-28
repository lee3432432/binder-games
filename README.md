# 커맨드 플레이 — 게임 포털

binder-games.vercel.app 에 배포되는 게임 모음 사이트입니다. (Vercel 자동 배포)

## 새 게임 추가하는 법
1. `games/게임이름/` 폴더를 만들고 게임의 `index.html`(과 필요한 파일)을 넣습니다.
2. 섬네일 이미지를 `assets/thumbs/게임이름.jpg` 로 넣습니다. (16:9 권장)
3. `games.json` 에 아래 항목을 하나 추가합니다.
   ```json
   { "id": "게임이름", "title": "게임 제목", "desc": "한 줄 설명", "thumb": "assets/thumbs/게임이름.jpg", "tags": ["3D"], "new": true }
   ```
4. GitHub에 올리면 1분 안에 사이트에 반영됩니다.

- 게임 주소: `binder-games.vercel.app/games/게임이름`
- 되돌리기: Vercel → Deployments → 이전 버전 → Promote

## 내 폰 찾기 (games/find-my-phone)
- 웹 푸시(VAPID)로 폰에 알림을 보내고, 위치는 ntfy.sh 공개 채널(코드별)로 주고받음
- 서버 함수: `api/ring.js` — Vercel 환경변수 `VAPID_PUBLIC_KEY`, `VAPID_PRIVATE_KEY`, `VAPID_SUBJECT` 필요
