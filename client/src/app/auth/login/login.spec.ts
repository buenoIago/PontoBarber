import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { violacoesCriticas } from '../../../testing/axe';
import { Login } from './login';

@Component({
  imports: [Login],
  template: `<app-login (voltar)="voltar.set(true)" />`,
})
class Host {
  readonly voltar = signal(false);
}

describe('Login', () => {
  let fixture: ComponentFixture<Host>;
  let http: HttpTestingController;

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
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should post the typed credentials and load the account', async () => {
    const elemento = await criar();
    const email = elemento.querySelector<HTMLInputElement>('input#login-email')!;
    const senha = elemento.querySelector<HTMLInputElement>('input#login-senha')!;

    email.value = 'usuario@exemplo.com';
    email.dispatchEvent(new Event('input', { bubbles: true }));
    senha.value = 'Senha123';
    senha.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();
    await fixture.whenStable();

    elemento.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const login = http.expectOne('/api/v1/auth/login');
    expect(login.request.method).toBe('POST');
    expect(login.request.body).toEqual({ email: 'usuario@exemplo.com', senha: 'Senha123' });
    login.flush(null);
    http.expectOne('/api/v1/auth/me').flush(null);
    http.verify();
  });

  it('should emit voltar when requested', async () => {
    const elemento = await criar();
    elemento.querySelector<HTMLButtonElement>('button.btn-link')?.click();
    await fixture.whenStable();
    expect(fixture.componentInstance.voltar()).toBe(true);
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
