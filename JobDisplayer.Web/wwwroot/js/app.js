(function () {
    const options = window.dashboardOptions || {};
    const hasResume = Boolean(options.hasResume);

    if (!hasResume) {
        document.querySelectorAll('.score').forEach((element) => {
            element.classList.add('score-muted');
            element.setAttribute('title', 'Upload a resume to enable matching scores.');
        });
    }

    document.querySelectorAll('[data-posted]').forEach((element) => {
        const iso = element.getAttribute('data-posted');
        if (!iso) {
            return;
        }

        const date = dayjs(iso);
        if (!date.isValid()) {
            return;
        }

        element.textContent = date.format('MMM D, YYYY h:mm A');
    });

    document.querySelectorAll('.score').forEach((element) => {
        const value = Number.parseFloat(element.getAttribute('data-score'));
        if (Number.isFinite(value)) {
            const normalized = Math.min(Math.max(value, 0), 100);
            element.style.setProperty('--score-value', normalized.toString());
        }
    });
})();
