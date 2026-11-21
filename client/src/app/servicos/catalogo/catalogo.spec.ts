import { provideZonelessChangeDetection, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { AuthService } from '../../auth/auth.service';
import { BarbeariaService } from '../../barbearia/barbearia.service';
import { violacoesCriticas } from '../../../testing/axe';
import { type Servico } from '../servicos.models';
import { ServicosService } from '../servicos.service';
import { CatalogoServicos } from './catalogo';

const servico: Servico = {
  id: 's1',
  barbeariaId: 'b1',
  nome: 'Corte',
  duracaoMinutos: 30,
  preco: 50,
  ativo: true,
  ehModelo: false,
};

describe('CatalogoServicos', () => {
  let fixture: ComponentFixture<CatalogoServicos>;

  async function criar(tipo: string, codigoConvite: string | null): Promise<HTMLElement> {
    const barbearia = signal({ id: 'b1', codigoConvite });
    await TestBed.configureTestingModule({
      imports: [CatalogoServicos],
      providers: [
        provideZonelessChangeDetection(),
        {
          provide: ServicosService,
          useValue: {
            servicos: signal([servico]),
            carregando: signal(false),
            erro: signal(null),
            listar: vi.fn(),
            criar: vi.fn(),
            atualizar: vi.fn(),
            remover: vi.fn(),
            limparErro: vi.fn(),
          },
        },
        {
          provide: BarbeariaService,
          useValue: {
            barbeariaId: signal('b1'),
            barbearia,
            carregando: signal(false),
            erro: signal(null),
          },
        },
        {
          provide: AuthService,
          useValue: { conta: signal({ tipo }), erro: signal(null), limparErro: vi.fn() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CatalogoServicos);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('should not offer management actions to customers', async () => {
    const elemento = await criar('Customer', null);

    expect(elemento.textContent).toContain('Corte');
    expect(elemento.querySelector('table')).not.toBeNull();
    expect(elemento.textContent).not.toContain('Novo serviço');
    expect(elemento.textContent).not.toContain('Editar');
    expect(elemento.textContent).not.toContain('Remover');
  });

  it('should offer full management to the owner', async () => {
    const elemento = await criar('Professional', 'ABC23456');

    expect(elemento.textContent).toContain('Novo serviço');
    expect(elemento.textContent).toContain('Editar');
    expect(elemento.textContent).toContain('Remover');
  });

  it('should offer only value editing to the barber', async () => {
    const elemento = await criar('Professional', null);

    expect(elemento.textContent).not.toContain('Novo serviço');
    expect(elemento.textContent).not.toContain('Remover');
    expect(elemento.textContent).toContain('Editar valores');
  });

  it('should not have critical axe violations', async () => {
    expect(await violacoesCriticas(await criar('Professional', 'ABC23456'))).toEqual([]);
  });
});
