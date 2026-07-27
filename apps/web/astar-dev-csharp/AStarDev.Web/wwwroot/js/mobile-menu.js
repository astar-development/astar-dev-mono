window.astarMobileMenu = (function () {
  let keydownHandler = null;

  function attach(drawer, trigger, dotNetRef) {
    document.body.style.overflow = 'hidden';

    keydownHandler = function (event) {
      if (event.key === 'Escape') {
        event.preventDefault();
        dotNetRef.invokeMethodAsync('CloseFromJsAsync');
        return;
      }

      if (event.key !== 'Tab') {
        return;
      }

      const focusable = drawer.querySelectorAll('a[href], button:not([disabled]), [tabindex]:not([tabindex="-1"])');
      if (focusable.length === 0) {
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];

      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener('keydown', keydownHandler);

    const firstFocusable = drawer.querySelector('a[href], button:not([disabled])');
    if (firstFocusable) {
      firstFocusable.focus();
    }
  }

  function detach(trigger) {
    document.body.style.overflow = '';
    if (keydownHandler) {
      document.removeEventListener('keydown', keydownHandler);
      keydownHandler = null;
    }
    if (trigger) {
      trigger.focus();
    }
  }

  return { attach: attach, detach: detach };
})();
