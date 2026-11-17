// 타자치는 친구들 — 사진으로 "내 얼굴 캐릭터" 만들기
// POST ?a=make  {uid,key,photo}  → OpenAI 이미지 편집으로 우리 그림체 얼굴 생성 (사진은 저장하지 않음)
// POST ?a=save  {uid,key,png}    → 완성된 얼굴 조각 저장, 프로필에 face 버전 기록
// GET  ?u=uid                    → 저장된 얼굴 조각 PNG (앱·웹이 받아서 캐릭터에 합성)
const crypto = require('crypto');
const URL_ = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
const TOKEN = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;
const OPENAI = process.env.OPENAI_API_KEY;
const DAILY = 3;

async function redis(cmds) {
  const r = await fetch(URL_ + '/pipeline', { method: 'POST', headers: { Authorization: 'Bearer ' + TOKEN, 'Content-Type': 'application/json' }, body: JSON.stringify(cmds) });
  if (!r.ok) throw new Error('redis ' + r.status);
  return (await r.json()).map(x => { if (x.error) throw new Error(x.error); return x.result; });
}
const sha = s => crypto.createHash('sha256').update(String(s)).digest('hex');
const okUid = u => /^[a-f0-9]{16,40}$/.test(u || '');
const kstDate = () => new Date(Date.now() + 9 * 3600e3).toISOString().slice(0, 10);
async function auth(uid, key) { if (!okUid(uid) || !key) return false; const [h] = await redis([['HGET', 'desk:p:' + uid, 'sh']]); return !!h && h === sha(key); }
function b64(dataUrl) { const m = /^data:image\/(png|jpeg|webp);base64,(.+)$/.exec(dataUrl || ''); return m ? { type: m[1], buf: Buffer.from(m[2], 'base64') } : null; }

const PROMPT = `Image 1 is our cartoon character illustration. Image 2 is a photo of a real person.
Redraw image 1 EXACTLY: identical composition, framing, camera angle and canvas; the same green crocodile hood with white teeth in exactly the same place and size; the same wooden desk edge, keyboard and bottle; the same art style (thick clean black outlines, flat cel colors, soft pink blush, cute chibi anime look).
Change ONLY the face and hair visible inside the hood opening so the character clearly resembles the person in image 2: hair color and hairstyle/bangs, eyebrow shape, eye shape, face shape, skin tone, and glasses or facial hair if the person has them.
Keep the face in the same position and size inside the hood, still peeking over the desk. Keep it a cute cartoon, not realistic. Transparent background outside the drawing. No text.`;

async function make(b, host) {
  if (!OPENAI) return [503, { error: '아직 AI 키가 연결되지 않았어요 (관리자 설정 필요)' }];
  if (!(await auth(b.uid, b.key))) return [403, { error: '앱에서 캐릭터를 클릭해 열어주세요' }];
  const photo = b64(b.photo); if (!photo || photo.buf.length > 4e6) return [400, { error: '사진을 다시 골라주세요 (4MB 이하)' }];
  const day = 'desk:face:n:' + b.uid + ':' + kstDate();
  const [n] = await redis([['INCR', day], ['EXPIRE', day, 172800]]);
  if (n > DAILY) return [429, { error: `오늘은 ${DAILY}번까지만 만들 수 있어요. 내일 다시 해주세요!` }];
  const ref = await (await fetch('https://' + host + '/desk/face_ref.png')).arrayBuffer();
  const fd = new FormData();
  fd.append('model', 'gpt-image-1');
  fd.append('image[]', new Blob([ref], { type: 'image/png' }), 'character.png');
  fd.append('image[]', new Blob([photo.buf], { type: 'image/' + photo.type }), 'photo.' + (photo.type === 'jpeg' ? 'jpg' : photo.type));
  fd.append('prompt', PROMPT);
  fd.append('size', '1536x1024'); fd.append('quality', 'medium'); fd.append('background', 'transparent'); fd.append('input_fidelity', 'high'); fd.append('n', '1');
  const r = await fetch('https://api.openai.com/v1/images/edits', { method: 'POST', headers: { Authorization: 'Bearer ' + OPENAI }, body: fd });
  const j = await r.json().catch(() => ({}));
  if (!r.ok || !j.data || !j.data[0]) {
    await redis([['DECR', day]]);
    const msg = (j.error && j.error.message) || ('HTTP ' + r.status);
    return [r.status === 400 ? 400 : 502, { error: /safety|moderation|policy/i.test(msg) ? '이 사진으로는 만들 수 없어요. 다른 사진으로 해주세요' : 'AI 그림 생성에 실패했어요: ' + msg.slice(0, 120) }];
  }
  return [200, { ok: true, image: 'data:image/png;base64,' + j.data[0].b64_json, left: Math.max(0, DAILY - n) }];
}

async function save(b) {
  if (!(await auth(b.uid, b.key))) return [403, { error: '앱에서 캐릭터를 클릭해 열어주세요' }];
  if (b.png === null) { await redis([['DEL', 'desk:face:img:' + b.uid], ['HDEL', 'desk:p:' + b.uid, 'face']]); return [200, { ok: true, face: '' }]; }
  const img = b64(b.png); if (!img || img.type !== 'png' || img.buf.length > 900000) return [400, { error: '얼굴 그림이 너무 커요' }];
  const ver = Date.now().toString(36);
  await redis([['SET', 'desk:face:img:' + b.uid, img.buf.toString('base64')], ['HSET', 'desk:p:' + b.uid, 'face', ver]]);
  return [200, { ok: true, face: ver }];
}

module.exports = async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  if (req.method === 'OPTIONS') return res.status(204).end();
  if (!URL_ || !TOKEN) return res.status(503).json({ error: 'db not connected' });
  const q = req.query || {};
  try {
    if (req.method === 'GET') {
      if (!okUid(q.u)) return res.status(400).json({ error: 'bad id' });
      const [s] = await redis([['GET', 'desk:face:img:' + q.u]]);
      if (!s) return res.status(404).json({ error: 'no face' });
      res.setHeader('Content-Type', 'image/png'); res.setHeader('Cache-Control', 'public, max-age=31536000, immutable');
      return res.status(200).send(Buffer.from(s, 'base64'));
    }
    let body = req.body; if (typeof body === 'string') { try { body = JSON.parse(body); } catch (e) { body = {}; } }
    res.setHeader('Cache-Control', 'no-store');
    const out = q.a === 'make' ? await make(body || {}, req.headers.host) : q.a === 'save' ? await save(body || {}) : [400, { error: 'unknown action' }];
    res.status(out[0]).json(out[1]);
  } catch (e) { res.status(500).json({ error: String(e.message || e) }); }
};
module.exports.config = { maxDuration: 60 };
