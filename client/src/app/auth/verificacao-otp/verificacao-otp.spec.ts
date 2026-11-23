import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AuthService } from '../auth.service';
import { violacoesCriticas } from '../../../testing/axe';
import { VerificacaoOtp } from './verificacao-otp';

@Component({
  imports: [VerificacaoOtp],
  template: `<app-verificacao-otp (voltar)="voltar.set(true)" />`,
})
class Host {
  readonly voltar = signal(false);
}

describe('VerificacaoOtp', () => {
  let fixture: ComponentFixture<Host>;
  let http: HttpTestingController;
  let auth: AuthService;

  async function criar(): Promise<HTMLElement> {
    await TestBed.configureTestingModule({
      imports: [Host],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(Host);
    http = TestBed.inject(HttpTestingController);
    auth = TestBed.inject(AuthService);
    auth.solicitarCodigo('cliente@exemplo.com');
    http.expectOne('/api/v1/auth/otp/request').flush(null, {
      status: 202,
      statusText: 'Accepted',
    });
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should show the destination e-mail and accept only six digits', async () => {
    const elemento = await criar();
    expect(elemento.textContent).toContain('cliente@exemplo.com');

    const campo = elemento.querySelector<HTMLInputElement>('input#otp-codigo')!;
    campo.value = '12a3456789';
    campo.dispatchEvent(new Event('input', { bubbles: true }));
    await fixture.whenStable();
    expect(campo.value).toBe('123456');
  });

  it('should verify the typed code and load the account', async () => {
    const elemento = await criar();
    const campo = elemento.querySelector<HTMLInputElement>('input#otp-codigo')!;
    campo.value = '123456';
    campo.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    elemento.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const verificacao = http.expectOne('/api/v1/auth/otp/verify');
    expect(verificacao.request.body).toEqual({ email: 'cliente@exemplo.com', codigo: '123456' });
    verificacao.flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/v1/auth/me').flush({
      id: '1',
      nomeCompleto: 'Cliente',
      email: 'cliente@exemplo.com',
      telefone: null,
      tipo: 'Customer',
    });
    expect(auth.emailOtp()).toBeNull();
    http.verify();
  });

  it('should request another code on resend', async () => {
    const elemento = await criar();
    const botoes = [...elemento.querySelectorAll<HTMLButtonElement>('app-button button')];
    botoes.find((botao) => botao.textContent?.trim() === 'Reenviar código')!.click();
    await fixture.whenStable();

    const reenvio = http.expectOne('/api/v1/auth/otp/request');
    expect(reenvio.request.body).toEqual({ email: 'cliente@exemplo.com' });
    reenvio.flush(null, { status: 202, statusText: 'Accepted' });
    http.verify();
  });

  it('should clear the OTP step and emit voltar on cancel', async () => {
    const elemento = await criar();
    elemento.querySelector<HTMLButtonElement>('button.btn-link')?.click();
    await fixture.whenStable();

    expect(auth.emailOtp()).toBeNull();
    expect(fixture.componentInstance.voltar()).toBe(true);
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
