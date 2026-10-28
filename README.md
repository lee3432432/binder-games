# 커맨드 플레이 — 게임 포털

games.teambinder.ai 에 배포되는 게임 모음 사이트입니다. (Vercel 자동 배포)

## 새 게임 추가하는 법
1. `games/게임이름/` 폴더를 만들고 게임의 `index.html`(과 필요한 파일)을 넣습니다.
2. 섬네일 이미지를 `assets/thumbs/게임이름.jpg` 로 넣습니다. (16:9 권장)
3. `games.json` 에 아래 항목을 하나 추가합니다.
   ```json
   { "id": "게임이름", "title": "게임 제목", "desc": "한 줄 설명", "thumb": "assets/thumbs/게임이름.jpg", "tags": ["3D"], "new": true }
   ```
4. GitHub에 올리면 1분 안에 사이트에 반영됩니다.

- 게임 주소: `games.teambinder.ai/games/게임이름`
- 되돌리기: Vercel → Deployments → 이전 버전 → Promote
