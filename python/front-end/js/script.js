document.addEventListener('DOMContentLoaded', () => {

    // FAQ — accordion
    const faqItems = document.querySelectorAll('.faq-item');

    faqItems.forEach(item => {
      item.addEventListener('toggle', () => {
        if (item.open) {
          faqItems.forEach(other => {
            if (other !== item) other.open = false;
          });
        }
      });
    });

    // Cartão — efeito 3D com o mouse
    const card = document.querySelector('.card');
    const cardArea = document.querySelector('.hero-card-area');
    const hasHover = window.matchMedia('(hover: hover)').matches;

    if (card && cardArea && hasHover) {
      cardArea.addEventListener('mousemove', (e) => {
        const rect = cardArea.getBoundingClientRect();
        const x = (e.clientX - rect.left) / rect.width - 0.5;
        const y = (e.clientY - rect.top) / rect.height - 0.5;

        const rotateY = -10 + (x * 12);
        const rotateX = 5 - (y * 12);

        card.style.transform =
          `rotateY(${rotateY}deg) rotateX(${rotateX}deg) translateY(-4px)`;
      });

      cardArea.addEventListener('mouseleave', () => {
        card.style.transform = '';
      });
    }

    // Scroll reveal
    const revealTargets = document.querySelectorAll(
      '.feature-main, .stats-editorial, .quote-section, .pricing-minimal, .faq-linear'
    );

    revealTargets.forEach(el => {
      el.style.opacity = '0';
      el.style.transform = 'translateY(20px)';
      el.style.transition = 'opacity 0.7s ease, transform 0.7s ease';
    });

    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.style.opacity = '1';
          entry.target.style.transform = 'translateY(0)';
          observer.unobserve(entry.target);
        }
      });
    }, {
      threshold: 0.1,
      rootMargin: '0px 0px -50px 0px'
    });

    revealTargets.forEach(el => observer.observe(el));

  });
