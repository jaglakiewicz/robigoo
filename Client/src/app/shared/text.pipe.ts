/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { Pipe, PipeTransform } from '@angular/core';
import { TextService } from '../services/text.service';

@Pipe({
  name: 'text',
  pure: true
})
export class TextPipe implements PipeTransform {
  constructor(private textService: TextService) {}

  transform(key: string | null | undefined): string {
    if (!key) {
      return '';
    }
    return this.textService.get(key);
  }
}
