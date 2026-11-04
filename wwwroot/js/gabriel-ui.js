/* NEO DRIVE: progressive enhancement only; never replaces MVC navigation or POST. */
(() => {
    'use strict';
    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const finePointer = window.matchMedia('(hover: hover) and (pointer: fine)');

    if ('IntersectionObserver' in window) {
        const revealObserver = new IntersectionObserver(entries => {
            let stagger = 0;
            for (const entry of entries) {
                if (!entry.isIntersecting) continue;
                if (!reducedMotion.matches) {
                    entry.target.style.setProperty('--ad-delay', `${Math.min(stagger++, 3) * 75}ms`);
                    entry.target.classList.add('ad-reveal-visible');
                }
                revealObserver.unobserve(entry.target);
            }
        }, { threshold: 0.08 });
        document.querySelectorAll('[data-ad-reveal]').forEach(el => revealObserver.observe(el));
    }

    document.querySelectorAll('[data-ad-tilt]').forEach(card => {
        let frame = 0;
        const reset = () => {
            cancelAnimationFrame(frame);
            frame = 0;
            ['--ad-rx', '--ad-ry', '--ad-px', '--ad-py', '--ad-x', '--ad-y'].forEach(key => card.style.removeProperty(key));
        };
        card.addEventListener('pointermove', event => {
            if (reducedMotion.matches || !finePointer.matches || event.pointerType === 'touch' || frame) return;
            const clientX = event.clientX;
            const clientY = event.clientY;
            frame = requestAnimationFrame(() => {
                frame = 0;
                const rect = card.getBoundingClientRect();
                const x = Math.max(0, Math.min(1, (clientX - rect.left) / rect.width));
                const y = Math.max(0, Math.min(1, (clientY - rect.top) / rect.height));
                card.style.setProperty('--ad-rx', `${(0.5 - y) * 3}deg`);
                card.style.setProperty('--ad-ry', `${(x - 0.5) * 3}deg`);
                card.style.setProperty('--ad-px', `${(x - 0.5) * 5}px`);
                card.style.setProperty('--ad-py', `${(y - 0.5) * 5}px`);
                card.style.setProperty('--ad-x', `${x * 100}%`);
                card.style.setProperty('--ad-y', `${y * 100}%`);
            });
        }, { passive: true });
        card.addEventListener('pointerleave', reset);
        card.addEventListener('pointercancel', reset);
        reducedMotion.addEventListener('change', reset);
        finePointer.addEventListener('change', reset);
    });

    const form = document.getElementById('formDiagnostico');
    if (!form) return;
    const symptom = form.querySelector('[name="Sintoma"]');
    const counter = form.querySelector('[data-ad-counter-for="Sintoma"]');
    const button = document.getElementById('botaoDiagnosticar');
    const label = button?.querySelector('[data-ad-button-label]');
    const status = document.getElementById('ad-submit-status');
    if (!symptom || !counter || !button || !label || !status) return;
    const idleLabel = label.textContent;
    let submitting = false;
    const updateCount = () => { counter.textContent = `${symptom.value.length.toLocaleString('pt-BR')} / 1.000`; };
    const resetForm = () => {
        submitting = false;
        button.disabled = false;
        label.textContent = idleLabel;
        form.removeAttribute('aria-busy');
        status.textContent = '';
        updateCount();
    };
    symptom.addEventListener('input', updateCount);
    form.addEventListener('reset', () => requestAnimationFrame(resetForm));
    // Restores the button when Back returns this document from the browser's cache.
    window.addEventListener('pageshow', resetForm);
    updateCount();
    form.addEventListener('submit', event => {
        if (submitting) { event.preventDefault(); return; }
        if (event.defaultPrevented) return;
        const jq = window.jQuery;
        if (jq && typeof jq.fn.valid === 'function' && !jq(form).valid()) {
            event.preventDefault();
            return;
        }
        if (!form.checkValidity()) {
            event.preventDefault();
            form.reportValidity();
            return;
        }
        // No fake progress, stages, fetch, timers or changes to the submitted fields.
        submitting = true;
        button.disabled = true;
        label.textContent = 'Aguardando resultado…';
        form.setAttribute('aria-busy', 'true');
        status.textContent = 'Consulta enviada. Aguarde a resposta do servidor.';
    });
})();
