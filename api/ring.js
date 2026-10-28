const webpush = require('web-push');

module.exports = async (req, res) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  if (req.method === 'OPTIONS') return res.status(204).end();
  if (req.method !== 'POST') return res.status(405).json({ error: 'POST only' });
  const { VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY, VAPID_SUBJECT } = process.env;
  if (!VAPID_PUBLIC_KEY || !VAPID_PRIVATE_KEY) return res.status(500).json({ error: 'VAPID keys not set' });
  webpush.setVapidDetails(VAPID_SUBJECT || 'mailto:hello@teambinder.ai', VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY);
  let body = req.body;
  if (typeof body === 'string') { try { body = JSON.parse(body); } catch (e) { body = {}; } }
  const { sub, code, sound } = body || {};
  if (!sub || !sub.endpoint) return res.status(400).json({ error: 'subscription missing' });
  const payload = JSON.stringify({ title: '📱 폰 찾는 중!', body: '여기 눌러서 "저 여기 있어요!" 하기', code: code || '', sound: sound || 'siren', t: Date.now() });
  try {
    await webpush.sendNotification(sub, payload, { TTL: 60, urgency: 'high' });
    res.status(200).json({ ok: true });
  } catch (e) {
    res.status(e.statusCode || 500).json({ ok: false, error: String(e.body || e.message || e), statusCode: e.statusCode });
  }
};
