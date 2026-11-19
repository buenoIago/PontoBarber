// jsdom não implementa o modal nativo; polyfill mínimo para os testes de diálogo.
if (typeof HTMLDialogElement !== 'undefined' && !HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = function showModal(): void {
    this.setAttribute('open', '');
  };
}

if (typeof HTMLDialogElement !== 'undefined' && !HTMLDialogElement.prototype.close) {
  HTMLDialogElement.prototype.close = function close(): void {
    this.removeAttribute('open');
    this.dispatchEvent(new Event('close'));
  };
}
