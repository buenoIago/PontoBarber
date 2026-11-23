import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component, provideZonelessChangeDetection } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { AuthService } from '../auth.service';
import { violacoesCriticas } from '../../../testing/axe';
import { Conta } from './conta';

@Component({
  imports: [Conta],
  template: `<app-conta />`,
})
class Host {}

describe('Conta', () => {
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
    TestBed.inject(AuthService).carregarConta();
    http.expectOne('/api/v1/auth/me').flush({
      id: '1',
      nomeCompleto: 'Nome Teste',
      email: 'usuario@exemplo.com',
      telefone: '11999999999',
      tipo: 'Customer',
    });
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should display the authenticated account data', async () => {
    const elemento = await criar();
    expect(elemento.textContent).toContain('Nome Teste');
    expect(elemento.textContent).toContain('usuario@exemplo.com');
    expect(elemento.textContent).toContain('Customer');
    http.verify();
  });

  it('should post logout and change-password requests', async () => {
    const elemento = await criar();

    const botoes = [...elemento.querySelectorAll<HTMLButtonElement>('app-button button')];
    botoes.find((botao) => botao.textContent?.trim() === 'Sair')!.click();
    await fixture.whenStable();
    expect(http.expectOne('/api/v1/auth/logout').request.method).toBe('POST');

    const valores: Record<string, string> = {
      senhaAtual: 'Senha123',
      novaSenha: 'NovaSenha123',
      confirmarNovaSenha: 'NovaSenha123',
    };
    for (const [id, valor] of Object.entries(valores)) {
      const campo = elemento.querySelector<HTMLInputElement>(`input#${id}`)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input', { bubbles: true }));
    }
    await fixture.whenStable();

    elemento.querySelector<HTMLFormElement>('form#formulario-senha')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    expect(http.expectOne('/api/v1/auth/change-password').request.method).toBe('POST');
    http.verify();
  });

  it('should update the customer profile through the API', async () => {
    const elemento = await criar();

    for (const [id, valor] of Object.entries({
      'perfil-nome': 'Nome Novo',
      'perfil-email': 'novo@exemplo.com',
      'perfil-telefone': '11888887777',
    })) {
      const campo = elemento.querySelector<HTMLInputElement>(`input#${id}`)!;
      campo.value = valor;
      campo.dispatchEvent(new Event('input', { bubbles: true }));
    }
    await fixture.whenStable();

    elemento.querySelector<HTMLFormElement>('form')?.dispatchEvent(new Event('submit'));
    await fixture.whenStable();

    const req = http.expectOne('/api/v1/me/customer-profile');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({
      nomeCompleto: 'Nome Novo',
      email: 'novo@exemplo.com',
      telefone: '11888887777',
    });
    req.flush({ id: '1', nomeCompleto: 'Nome Novo', email: 'novo@exemplo.com', telefone: '11888887777' });
    await fixture.whenStable();
    expect(elemento.textContent).toContain('Dados atualizados.');
    http.verify();
  });

  it('should not have critical axe violations when rendered', async () => {
    expect(await violacoesCriticas(await criar())).toEqual([]);
  });
});
