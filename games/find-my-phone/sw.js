self.addEventListener('install', e => self.skipWaiting());
self.addEventListener('activate', e => e.waitUntil(self.clients.claim()));
self.addEventListener('push', e => {
  let d = {}; try { d = e.data ? e.data.json() : {}; } catch (err) { d = { body: e.data && e.data.text() }; }
  const url = '/games/find-my-phone/?mode=phone&alarm=1&code=' + encodeURIComponent(d.code || '') + '&sound=' + encodeURIComponent(d.sound || 'siren') + '&t=' + Date.now();
  e.waitUntil(Promise.all([
    self.registration.showNotification(d.title || '📱 폰 찾는 중!', {
      body: d.body || '여기 눌러서 "저 여기 있어요!" 하기',
      icon: '/games/find-my-phone/icon.png', badge: '/games/find-my-phone/icon.png',
      tag: 'find-my-phone', renotify: true, requireInteraction: true,
      vibrate: [500, 200, 500, 200, 500, 200, 1000],
      data: { url }
    }),
    // 이미 열려 있는 창이 있으면 바로 알람 켜기
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(cs => {
      cs.forEach(c => c.postMessage({ type: 'ALARM', code: d.code || '', sound: d.sound || 'siren' }));
    })
  ]));
});
self.addEventListener('notificationclick', e => {
  e.notification.close();
  const url = (e.notification.data && e.notification.data.url) || '/games/find-my-phone/?mode=phone&alarm=1';
  e.waitUntil(self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then(cs => {
    for (const c of cs) { if ('focus' in c) { c.postMessage({ type: 'ALARM' }); c.navigate && c.navigate(url); return c.focus(); } }
    return self.clients.openWindow(url);
  }));
});
