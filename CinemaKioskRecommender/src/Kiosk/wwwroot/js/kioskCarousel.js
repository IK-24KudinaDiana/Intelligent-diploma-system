window.kioskCarousel = {
  scrollByCard: (el, direction) => {
    if (!el) return;
    const card = el.querySelector('.kiosk-might-like-card');
    const gap = parseFloat(getComputedStyle(el).columnGap || getComputedStyle(el).gap) || 16;
    const amount = card ? (card.offsetWidth + gap) * direction : 280 * direction;
    el.scrollBy({ left: amount, behavior: 'smooth' });
  }
};
