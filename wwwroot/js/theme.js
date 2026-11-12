/* Alternância entre modo claro e escuro.
   O tema inicial já é aplicado por um script curto no <head> do _Layout (evita "piscar" escuro→claro).
   Ordem de decisão: escolha salva pelo usuário > preferência do sistema operacional > escuro. */
(() => {
    'use strict';
    const KEY = 'ad-theme';
    const root = document.documentElement;
    const button = document.querySelector('[data-ad-theme-toggle]');
    const meta = document.querySelector('meta[name="theme-color"]');
    const colors = { dark: '#070B14', light: '#F3F6FA' };
    const systemLight = window.matchMedia ? window.matchMedia('(prefers-color-scheme: light)') : null;

    const saved = () => { try { return localStorage.getItem(KEY); } catch { return null; } };

    const apply = theme => {
        root.setAttribute('data-theme', theme);
        if (meta) meta.setAttribute('content', colors[theme]);
        if (button) {
            const label = theme === 'light' ? 'Ativar modo escuro' : 'Ativar modo claro';
            button.setAttribute('aria-label', label);
            button.setAttribute('title', label);
            button.setAttribute('aria-pressed', String(theme === 'light'));
        }
    };

    apply(root.getAttribute('data-theme') === 'light' ? 'light' : 'dark');

    if (button) {
        button.hidden = false;
        button.addEventListener('click', () => {
            const next = root.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
            apply(next);
            try { localStorage.setItem(KEY, next); } catch { /* navegação privada: vale só nesta página */ }
        });
    }

    // Se o usuário nunca escolheu, acompanha a mudança do sistema em tempo real.
    systemLight?.addEventListener?.('change', event => {
        const choice = saved();
        if (choice !== 'light' && choice !== 'dark') apply(event.matches ? 'light' : 'dark');
    });
})();
