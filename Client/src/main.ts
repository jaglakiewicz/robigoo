/*
 * ROBIGOO FIELD SPRAYER CONTROL STATION
 * Copyright (c) 2025 Wojciech Salamon <wojciech.salamon@yahoo.com>
 * All rights reserved. Unauthorized distribution or disclosure is prohibited.
*/

import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';

// Clear session on application startup
// This ensures a fresh start and prevents leftover tokens from previous sessions
localStorage.removeItem('currentUser');
localStorage.removeItem('token');
sessionStorage.clear();

platformBrowserDynamic().bootstrapModule(AppModule)
  .catch(err => console.error(err));

