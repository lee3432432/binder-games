// 타자치는 친구들 — 기록·랭킹·캐릭터 API (Upstash Redis REST, 의존성 없음)
// 앱은 "누른 횟수"만 보냄. 어떤 키를 눌렀는지는 절대 받지 않음.
const crypto = require('crypto');

const URL_ = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
const TOKEN = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;
const BASES = ['bunny', 'dog', 'croc', 'shark', 'cat', 'none'];
const ITEMS = ['cola', 'americano', 'coffee', 'bananamilk', 'bubbletea', 'energy', 'ramen', 'strawberrymilk'];
const okFsel = f => /^(f[1-9]|own)?$/.test(f || '');
const ANIMAL = { bunny: '토끼', dog: '강아지', croc: '악어', shark: '상어', cat: '고양이', none: '친구' };
const DAY_MAX = 400000; // 하루 최대 인정 횟수 (조작 방지)

async function redis(cmds) {
  const r = await fetch(URL_ + '/pipeline', { method: 'POST', headers: { Authorization: 'Bearer ' + TOKEN, 'Content-Type': 'application/json' }, body: JSON.stringify(cmds) });
  if (!r.ok) throw new Error('redis ' + r.status);
  const out = await r.json();
  return out.map(x => { if (x.error) throw new Error(x.error); return x.result; });
}
const sha = s => crypto.createHash('sha256').update(String(s)).digest('hex');
const kstDate = (off = 0) => new Date(Date.now() + 9 * 3600e3 - off * 86400e3).toISOString().slice(0, 10);
const okDate = d => /^\d{4}-\d{2}-\d{2}$/.test(d) && d >= kstDate(8) && d <= kstDate(-1);
const okUid = u => /^[a-f0-9]{16,40}$/.test(u || '');
const okColor = c => /^#[0-9a-fA-F]{6}$/.test(c || '');
const clean = s => String(s || '').replace(/[<>"'`\\\n\r\t]/g, '').trim().slice(0, 12);
function profileOut(h) {
  return { name: h.name || '익명', base: BASES.includes(h.base) ? h.base : 'croc', color: okColor(h.color) ? h.color : '', amt: Math.max(0, Math.min(1, +h.amt || 0)), face: /^[a-z0-9]{4,12}$/.test(h.face || '') ? h.face : '', item: h.item === undefined ? 'cola' : (ITEMS.includes(h.item) ? h.item : ''), fsel: okFsel(h.fsel) ? (h.fsel || '') : '', own: /^[a-z0-9]{4,12}$/.test(h.own || '') ? h.own : '' };
}
const toObj = arr => { const o = {}; for (let i = 0; arr && i < arr.length; i += 2) o[arr[i]] = arr[i + 1]; return o; };

async function auth(uid, key, create) {
  const [h] = await redis([['HGET', 'desk:p:' + uid, 'sh']]);
  if (!h) {
    if (!create) return false;
    const base = BASES.includes(create.base) ? create.base : 'croc';
    const name = clean(create.name) || ('익명의 ' + ANIMAL[base] + (1000 + Math.floor(Math.random() * 9000)));
    await redis([['HSETNX', 'desk:p:' + uid, 'sh', sha(key)], ['HSET', 'desk:p:' + uid, 'name', name, 'base', base, 'created', Date.now()], ['SADD', 'desk:users', uid]]);
    return true;
  }
  return h === sha(key);
}

async function sync(b) {
  const { uid, key } = b; if (!okUid(uid) || !key) return [400, { error: 'bad id' }];
  if (!(await auth(uid, key, { base: b.base, name: b.name }))) return [403, { error: 'bad key' }];
  const cmds = [], days = b.days || {};
  for (const d of Object.keys(days).slice(0, 10)) {
    if (!okDate(d)) continue; const v = days[d] || []; const k = Math.max(0, Math.min(DAY_MAX, Math.floor(+v[0] || 0))), c = Math.max(0, Math.min(DAY_MAX, Math.floor(+v[1] || 0)));
    cmds.push(['HSET', 'desk:d:' + uid, d, k + ',' + c], ['ZADD', 'desk:z:k:' + d, k, uid], ['ZADD', 'desk:z:c:' + d, c, uid], ['EXPIRE', 'desk:z:k:' + d, 3456000], ['EXPIRE', 'desk:z:c:' + d, 3456000]);
  }
  const hours = b.hours || {};   // 시간대별: 날짜 → [타자 0~23시, 클릭 0~23시] (앱 v14부터)
  for (const d of Object.keys(hours).slice(0, 3)) {
    const v = hours[d]; if (!okDate(d) || !Array.isArray(v) || v.length !== 48) continue;
    cmds.push(['HSET', 'desk:h:' + uid, d, v.map(x => Math.max(0, Math.min(DAY_MAX, Math.floor(+x || 0)))).join(',')]);
  }
  cmds.push(['HGETALL', 'desk:d:' + uid], ['HGETALL', 'desk:p:' + uid], ['HSET', 'desk:p:' + uid, 'seen', Date.now()]);
  const r = await redis(cmds); const all = toObj(r[r.length - 3]); let K = 0, C = 0;
  for (const v of Object.values(all)) { const [k, c] = String(v).split(','); K += +k || 0; C += +c || 0; }
  await redis([['ZADD', 'desk:z:k:all', K, uid], ['ZADD', 'desk:z:c:all', C, uid]]);
  return [200, { ok: true, profile: profileOut(toObj(r[r.length - 2])), total: [K, C] }];
}

async function profile(b) {
  const { uid, key } = b; if (!okUid(uid) || !key) return [400, { error: 'bad id' }];
  if (!(await auth(uid, key, null))) return [403, { error: 'bad key' }];
  const set = [];
  if (b.name !== undefined) { const n = clean(b.name); if (n.length < 1) return [400, { error: '닉네임을 1~12자로 적어주세요' }]; set.push('name', n); }
  if (b.base !== undefined) { if (!BASES.includes(b.base)) return [400, { error: 'bad base' }]; set.push('base', b.base); }
  if (b.color !== undefined) set.push('color', okColor(b.color) ? b.color : '');
  if (b.amt !== undefined) set.push('amt', String(Math.max(0, Math.min(1, +b.amt || 0))));
  if (b.item !== undefined) { if (b.item !== '' && !ITEMS.includes(b.item)) return [400, { error: 'bad item' }]; set.push('item', b.item); }
  if (b.fsel !== undefined) { if (!okFsel(b.fsel)) return [400, { error: 'bad face' }]; set.push('fsel', b.fsel); }
  if (set.length) await redis([['HSET', 'desk:p:' + uid, ...set]]);
  const [h] = await redis([['HGETALL', 'desk:p:' + uid]]);
  return [200, { ok: true, profile: profileOut(toObj(h)) }];
}

async function user(q) {
  const uid = q.u; if (!okUid(uid)) return [400, { error: 'bad id' }];
  const t = kstDate();
  const [p, d, rk, rc, ak, ac, nk, nc, hh] = await redis([['HGETALL', 'desk:p:' + uid], ['HGETALL', 'desk:d:' + uid], ['ZREVRANK', 'desk:z:k:' + t, uid], ['ZREVRANK', 'desk:z:c:' + t, uid], ['ZREVRANK', 'desk:z:k:all', uid], ['ZREVRANK', 'desk:z:c:all', uid], ['ZCARD', 'desk:z:k:' + t], ['ZCARD', 'desk:z:k:all'], ['HGETALL', 'desk:h:' + uid]]);
  const ph = toObj(p); if (!ph.sh) return [404, { error: '아직 기록이 없어요' }];
  const days = {}; for (const [k, v] of Object.entries(toObj(d))) days[k] = String(v).split(',').map(Number);
  const hours = {}, hcut = kstDate(31); for (const [k, v] of Object.entries(toObj(hh))) if (k >= hcut) hours[k] = String(v).split(',').map(Number);
  const R = x => (x === null || x === undefined ? null : x + 1);
  return [200, { profile: profileOut(ph), days, hours, today: t, rank: { todayKeys: R(rk), todayClicks: R(rc), allKeys: R(ak), allClicks: R(ac), todayUsers: nk, allUsers: nc } }];
}

async function top(q) {
  const metric = q.m === 'c' ? 'c' : 'k', period = ['today', 'week', 'all'].includes(q.p) ? q.p : 'today';
  let key;
  if (period === 'all') key = 'desk:z:' + metric + ':all';
  else if (period === 'today') key = 'desk:z:' + metric + ':' + kstDate();
  else {
    key = 'desk:tmp:' + metric + ':week:' + kstDate();
    const [ex] = await redis([['EXISTS', key]]);
    if (!ex) { const src = [...Array(7)].map((_, i) => 'desk:z:' + metric + ':' + kstDate(i)); await redis([['ZUNIONSTORE', key, 7, ...src], ['EXPIRE', key, 120]]); }
  }
  const [arr, n] = await redis([['ZREVRANGE', key, 0, 49, 'WITHSCORES'], ['ZCARD', key]]);
  const ids = [], scores = []; for (let i = 0; i < (arr || []).length; i += 2) { if (+arr[i + 1] > 0) { ids.push(arr[i]); scores.push(+arr[i + 1]); } }
  const ps = ids.length ? await redis(ids.map(id => ['HMGET', 'desk:p:' + id, 'name', 'base', 'color', 'amt', 'face', 'item'])) : [];
  const list = ids.map((id, i) => ({ id: id.slice(0, 6), name: ps[i][0] || '익명', base: ps[i][1] || 'croc', color: ps[i][2] || '', amt: +ps[i][3] || 0, face: ps[i][4] ? id + '/' + ps[i][4] : '', item: ps[i][5] === null ? 'cola' : ps[i][5], v: scores[i] }));
  return [200, { period, metric, users: n || 0, list }];
}

module.exports = async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  res.setHeader('Cache-Control', 'no-store');
  if (req.method === 'OPTIONS') return res.status(204).end();
  if (!URL_ || !TOKEN) return res.status(503).json({ error: 'db not connected' });
  let body = req.body; if (typeof body === 'string') { try { body = JSON.parse(body); } catch (e) { body = {}; } }
  const q = req.query || {}; const a = q.a;
  try {
    let out;
    if (req.method === 'POST' && a === 'sync') out = await sync(body || {});
    else if (req.method === 'POST' && a === 'profile') out = await profile(body || {});
    else if (a === 'user') out = await user(q);
    else if (a === 'top') out = await top(q);
    else out = [400, { error: 'unknown action' }];
    res.status(out[0]).json(out[1]);
  } catch (e) { res.status(500).json({ error: String(e.message || e) }); }
};
