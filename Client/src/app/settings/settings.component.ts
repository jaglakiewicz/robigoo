import { Component, OnInit, OnDestroy, ElementRef, ViewChild } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { UserService, CreateUserRequest, UserDTO } from '../services/user.service';
import { NotificationService } from '../services/notification.service';
import { SVG_ICONS } from '../shared/svg-icons';
import { Step } from '../shared/components/step-indicator/step-indicator.component';
import { ScrollSpyDirective } from '../shared/directives/scroll-spy.directive';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit, OnDestroy {
  @ViewChild(ScrollSpyDirective) private scrollSpy!: ScrollSpyDirective;
  activeSection = 0;
  activeSubKey = '';
  currentStep = 0; // kept for compatibility
  private editUserListener: any;
  SVG_ICONS = SVG_ICONS; // Make SVG_ICONS available in template

  get saving(): boolean {
    return this.savingAppSettings || this.savingSettings;
  }

  steps: Step[] = [
    {
      id: 0, key: 'program', label: 'Ustawienia programu',
      children: [
        { id: 0, key: 'sub-org',          label: 'Jednostka' },
        { id: 0, key: 'sub-docs',         label: 'Dokumenty' },
        { id: 0, key: 'sub-protocol',     label: 'Protokól' },
        { id: 0, key: 'sub-register',     label: 'Rejestr' },
        { id: 0, key: 'sub-controlmarks', label: 'Znaki kontrolne' },
      ]
    },
    { id: 1, key: 'user',  label: 'Ustawienia uzytkownika' },
    { id: 2, key: 'admin', label: 'Zarzadzanie uzytkownikami' },
    { id: 3, key: 'sessions', label: 'Sesje uzytkowników' },
    { id: 4, key: 'activity', label: 'Dziennik aktywnosci' }
  ];

  // -- App Settings ------------------------------------------------------
  appSettings = {
    organization: {
      stationName: '', unitAuthorizationNumber: '',
      addressLine1: '', addressLine2: '', city: '', postalCode: '', postOffice: '',
      phone: '', email: '', taxId: '', regon: ''
    },
    documents: {
      protocol: {
        inspectionValidityYears: 3, numberPrefix: '', sequencePadding: 3,
        numberFormat: '{PREFIX}/{YEAR}/{SEQ}', header: '', footer: '', defaultXslTemplate: ''
      },
      register: { header: '', footer: '', defaultXslTemplate: '' },
      controlMarks: { header: '', footer: '', defaultXslTemplate: '' }
    }
  };

  savingAppSettings = false;

  readonly formatTokens = [
    { token: '{PREFIX}', hint: 'Prefiks (np. SKO)' },
    { token: '{YEAR}',   hint: 'Rok (np. 2025)' },
    { token: '{MONTH}',  hint: 'Miesiac (np. 06)' },
    { token: '{SEQ}',    hint: 'Numer sekwencyjny (np. 001)' },
    { token: '{SEQ4}',   hint: 'Numer 4-cyfrowy (np. 0001)' },
  ];

  // XSL Templates (per-document-type, loaded once)
  xslTemplates: { name: string; uploadedAt: string; size: number }[] = [];
  defaultXslTemplate = '';
  loadingXsl = false;
  uploadingXsl = false;
  xslError = '';
  xslSuccess = '';

  get protocolNumberPreview(): string {
    const now = new Date();
    const p = this.appSettings.documents.protocol;
    return (p.numberFormat || '{YEAR}/{SEQ}')
      .replace('{PREFIX}', p.numberPrefix || '')
      .replace('{YEAR}',   String(now.getFullYear()))
      .replace('{MONTH}',  String(now.getMonth() + 1).padStart(2, '0'))
      .replace('{SEQ}',    '1'.padStart(p.sequencePadding || 3, '0'))
      .replace('{SEQ4}',   '1'.padStart(4, '0'));
  }

  insertToken(token: string): void {
    this.appSettings.documents.protocol.numberFormat =
      (this.appSettings.documents.protocol.numberFormat || '') + token;
  }

  // User Settings
  userSettings = {
    login: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    theme: 'light',
    avatar: null as string | null
  };

  // Password change
  passwordForm = {
    oldPassword: '',
    newPassword: '',
    confirmPassword: ''
  };
  changePasswordError = '';
  changePasswordSuccess = false;
  changingPassword = false;
  showPasswordForm = false;

  // Settings save messages
  userSettingsError = '';
  userSettingsSuccess = false;
  savingSettings = false;
  avatarError = '';
  avatarSuccess = false;
  signatureImage: string | null = null;
  signatureError = '';
  signatureSuccess = false;

  // Admin Settings
  adminSettings = {
    enableLogs: true,
    logLevel: 'info',
    maxUsers: 100
  };

  // User Management
  users: UserDTO[] = [];
  showCreateUserForm = false;
  newUserForm = {
    login: '',
    password: '',
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    permissionNumber: '',
    role: 'user'
  };
  creatingUser = false;

  // Password confirmation dialog
  showPasswordConfirmDialog = false;
  confirmDialogAction: 'delete' | 'create' = 'delete';
  userToConfirm: UserDTO | null = null;
  confirmDialogPassword = '';
  confirmDialogError = '';
  confirmingAction = false;
  deletingUserId: number | null = null;

  // Role change
  updatingRoleUserId: number | null = null;

  // Session Management
  activeSessions: any[] = [];
  loadingSessions = false;
  terminatingSessionId: number | null = null;

  // Activity Logs
  userActivities: any[] = [];
  loadingActivities = false;
  selectedUserId: number | null = null;
  activityDateFrom: string = '';
  activityDateTo: string = '';

  constructor(
    private authService: AuthService,
    private userService: UserService,
    private notificationService: NotificationService,
    private sanitizer: DomSanitizer,
    private el: ElementRef,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    try {
      console.log('[Settings] Component initialized');
      
      // Listener for edit user settings event
      this.editUserListener = (event: any) => {
        this.scrollToSection(1);
      };
      window.addEventListener('editUserEvent', this.editUserListener);

      this.loadUserSettings();
      this.loadUsers();
      this.loadXslTemplates();
      this.loadAppSettings();
    } catch (error) {
      console.error('[Settings] Error during initialization:', error);
    }
  }

  ngOnDestroy(): void {
    window.removeEventListener('editUserEvent', this.editUserListener);
  }

  loadUserSettings(): void {
    const currentUser = this.authService.getCurrentUser();
    if (currentUser) {
      this.userSettings.login = currentUser.login;
      this.userSettings.firstName = currentUser.firstName;
      this.userSettings.lastName = currentUser.lastName;
      this.userSettings.email = currentUser.email;
      this.userSettings.phone = currentUser.phone;
      this.userSettings.permissionNumber = currentUser.permissionNumber;
      if (currentUser.avatarBase64) {
        this.userSettings.avatar = 'data:image/png;base64,' + currentUser.avatarBase64;
      }
      if ((currentUser as any).signatureBase64) {
        this.signatureImage = 'data:image/png;base64,' + (currentUser as any).signatureBase64;
      }
    }
  }

  loadUsers(): void {
    this.userService.getUsers().subscribe(
      (data: any) => {
        if (data && Array.isArray(data)) {
          this.users = data;
        } else {
          console.error('[Settings] Unexpected response format:', data);
        }
      },
      (error: any) => {
        console.error('[Settings] Error loading users:', error);
      }
    );
  }

  saveUserSettings(): void {
    this.userSettingsError = '';
    this.userSettingsSuccess = false;
    this.savingSettings = true;

    const currentUser = this.authService.getCurrentUser();
    const isAdmin = currentUser && currentUser.role === 'admin';

    // Regular users can only edit: email, phone, theme
    // Admins can also edit: firstName, lastName, permissionNumber
    const updateData: any = {
      email: this.userSettings.email,
      phone: this.userSettings.phone,
      theme: this.userSettings.theme
    };

    // Only admins can modify firstName, lastName, and permissionNumber
    if (isAdmin) {
      updateData.firstName = this.userSettings.firstName;
      updateData.lastName = this.userSettings.lastName;
      updateData.permissionNumber = this.userSettings.permissionNumber;
    }

    this.userService.updateProfile(updateData).subscribe(
      (response) => {
        console.log('[Settings] Profile updated:', response);
        this.notificationService.success('Ustawienia zostaly pomyslnie zapisane!');
        this.savingSettings = false;

        // Update current user in auth service
        const currentUser = this.authService.getCurrentUser();
        if (currentUser) {
          // Always update these fields
          currentUser.email = this.userSettings.email;
          currentUser.phone = this.userSettings.phone;
          
          // Update admin-only fields if user is admin
          if (isAdmin) {
            currentUser.firstName = this.userSettings.firstName;
            currentUser.lastName = this.userSettings.lastName;
            currentUser.permissionNumber = this.userSettings.permissionNumber;
          }

          sessionStorage.setItem('currentUser', JSON.stringify(currentUser));
        }
      },
      (error: any) => {
        console.error('[Settings] Error saving settings:', error);
        const errorMsg = error.error?.message || 'Blad podczas zapisywania ustawien';
        this.notificationService.error(errorMsg);
        this.savingSettings = false;
      }
    );
  }

  togglePasswordForm(): void {
    this.showPasswordForm = !this.showPasswordForm;
    if (!this.showPasswordForm) {
      // Clear form when closing
      this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
      this.changePasswordError = '';
    }
  }

  changePassword(): void {
    if (this.passwordForm.newPassword !== this.passwordForm.confirmPassword) {
      this.notificationService.error('Hasla nie pasuja do siebie');
      return;
    }

    if (this.passwordForm.newPassword.length < 6) {
      this.notificationService.error('Haslo musi miec co najmniej 6 znaków');
      return;
    }

    this.changingPassword = true;

    this.userService.changePassword({
      oldPassword: this.passwordForm.oldPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe(
      (response) => {
        console.log('[Settings] Password changed:', response);
        this.notificationService.success('Haslo zostalo zmienione pomyslnie!');
        this.changingPassword = false;
        this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
        this.showPasswordForm = false;
        this.passwordDrawerOpen = false;
      },
      (error: any) => {
        console.error('[Settings] Error changing password:', error);
        const errorMsg = error.error?.message || 'Blad podczas zmiany hasla';
        this.notificationService.error(errorMsg);
        this.changingPassword = false;
      }
    );
  }

  onAvatarSelected(event: any): void {
    const file: File = event.target.files[0];

    if (file) {
      // Validate file size (max 2MB)
      if (file.size > 2097152) { // 2MB
        this.avatarError = 'Plik jest za duzy (maksymalnie 2MB)';
        return;
      }

      // Validate file type
      if (!file.type.startsWith('image/')) {
        this.avatarError = 'Plik musi byc obrazem (JPG, PNG, GIF)';
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.userService.uploadAvatar(file).subscribe(
          (response) => {
            console.log('[Settings] Avatar uploaded:', response);
            // Reload current user to get updated avatar
            this.loadUserSettings();
            this.avatarSuccess = true;
            this.avatarError = '';
            setTimeout(() => { this.avatarSuccess = false; }, 3000);
          },
          (error: any) => {
            console.error('[Settings] Error uploading avatar:', error);
            this.avatarError = error.error?.message || 'Blad podczas przesylania avatara';
          }
        );
      };
      reader.readAsDataURL(file);
    }
  }

  onSignatureSelected(event: any): void {
    const file: File = event.target.files[0];
    if (!file) return;
    if (file.size > 2097152) { this.signatureError = 'Plik jest za duzy (max 2MB)'; return; }
    if (!file.type.startsWith('image/')) { this.signatureError = 'Plik musi byc obrazem'; return; }
    this.userService.uploadSignature(file).subscribe({
      next: (res) => {
        this.signatureImage = 'data:image/png;base64,' + res.signatureBase64;
        this.signatureSuccess = true;
        this.signatureError = '';
        setTimeout(() => { this.signatureSuccess = false; }, 3000);
      },
      error: (err) => { this.signatureError = err.error?.message || 'Blad podczas przeslania podpisu'; }
    });
  }

  passwordDrawerOpen = false;
  createUserDrawerOpen = false;

  openPasswordDrawer(): void {
    this.passwordForm = { oldPassword: '', newPassword: '', confirmPassword: '' };
    this.passwordDrawerOpen = true;
  }
  closePasswordDrawer(): void { this.passwordDrawerOpen = false; }

  openCreateUserDrawer(): void {
    this.resetCreateUserForm();
    this.createUserDrawerOpen = true;
  }
  closeCreateUserDrawer(): void { this.createUserDrawerOpen = false; }

  createUserFromDrawer(): void {
    this.createUser();
    if (!this.creatingUser) this.createUserDrawerOpen = false;
  }

  canSubmitPassword(): boolean {
    return !!this.passwordForm.oldPassword &&
           !!this.passwordForm.newPassword &&
           this.passwordForm.newPassword === this.passwordForm.confirmPassword &&
           this.passwordForm.newPassword.length >= 6;
  }

  getPasswordStrengthClass(): string {
    const p = this.passwordForm.newPassword;
    if (!p) return '';
    let score = 0;
    if (p.length >= 8) score++;
    if (p.length >= 12) score++;
    if (/[A-Z]/.test(p)) score++;
    if (/[0-9]/.test(p)) score++;
    if (/[^A-Za-z0-9]/.test(p)) score++;
    if (score <= 1) return 'strength-weak';
    if (score <= 3) return 'strength-medium';
    return 'strength-strong';
  }

  getPasswordStrengthLabel(): string {
    const c = this.getPasswordStrengthClass();
    if (c === 'strength-weak') return 'Slabe';
    if (c === 'strength-medium') return 'Srednie';
    return 'Silne';
  }

  scrollToSection(sectionId: number): void {
    const container = this.el.nativeElement.querySelector('.settings-scroll') as HTMLElement;
    const el = this.el.nativeElement.querySelector(`#section-${sectionId}`) as HTMLElement;
    if (container && el) {
      this.scrollSpy?.lock();
      this.activeSection = sectionId;
      const step = this.steps.find(s => s.id === sectionId);
      this.activeSubKey = step?.children?.[0]?.key ?? '';
      container.scrollTop = el.offsetTop - 4;
    }
  }

  scrollToSubSection(key: string): void {
    const container = this.el.nativeElement.querySelector('.settings-scroll') as HTMLElement;
    const el = this.el.nativeElement.querySelector(`#${key}`) as HTMLElement;
    if (container && el) {
      this.scrollSpy?.lock();
      this.activeSubKey = key;
      container.scrollTop = el.offsetTop - 4;
    }
  }

  onActiveSection(id: string): void {
    const num = parseInt(id.replace('section-', ''), 10);
    if (!isNaN(num) && num !== this.activeSection) {
      this.activeSection = num;
      // Reset sub-key to first child of the newly active section
      const step = this.steps.find(s => s.id === num);
      this.activeSubKey = step?.children?.[0]?.key ?? '';
    }
  }

  goToStep(stepId: number): void {
    this.scrollToSection(stepId);
  }

  get xslTemplateOptions() {
    return [
      { value: '', label: '— brak —' },
      ...this.xslTemplates.map(t => ({ value: t.name, label: t.name }))
    ];
  }

  loadAppSettings(): void {
    this.http.get<any>('/api/settings').subscribe({
      next: (data) => { this.appSettings = { ...this.appSettings, ...data }; },
      error: () => {}
    });
  }

  saveAppSettings(): void {
    this.savingAppSettings = true;
    this.http.put('/api/settings', this.appSettings).subscribe({
      next: () => { this.notificationService.success('Ustawienia programu zapisane'); this.savingAppSettings = false; },
      error: (err) => { this.notificationService.error(err.error?.message || 'Blad zapisu'); this.savingAppSettings = false; }
    });
  }

  saveAll(): void {
    this.saveAppSettings();
    this.saveUserSettings();
  }

  get visibleSteps(): Step[] {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser || currentUser.role !== 'admin') {
      // Non-admin users see only steps 0 and 1
      return [this.steps[0], this.steps[1]];
    }
    // Admin users see all steps
    return this.steps;
  }

  isCurrentUserAdmin(): boolean {
    const currentUser = this.authService.getCurrentUser();
    return currentUser && currentUser.role === 'admin';
  }

  toggleCreateUserForm(): void {
    this.showCreateUserForm = !this.showCreateUserForm;
    if (!this.showCreateUserForm) {
      this.resetCreateUserForm();
    }
  }

  resetCreateUserForm(): void {
    const currentUser = this.authService.getCurrentUser();
    
    this.newUserForm = {
      login: '',
      password: '',
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      permissionNumber: '',
      role: (currentUser && currentUser.login !== 'admin') ? 'user' : 'user'
    };
  }

  canSelectRoleWhenCreating(): boolean {
    const currentUser = this.authService.getCurrentUser();
    // Only master admin can select role when creating users
    return currentUser && currentUser.login === 'admin';
  }

  createUser(): void {
    if (!this.newUserForm.login || !this.newUserForm.password || !this.newUserForm.firstName) {
      this.notificationService.error('Login, haslo i imie sa wymagane');
      return;
    }

    this.creatingUser = true;

    const newUser: CreateUserRequest = {
      login: this.newUserForm.login,
      password: this.newUserForm.password,
      firstName: this.newUserForm.firstName,
      lastName: this.newUserForm.lastName,
      email: this.newUserForm.email,
      phone: this.newUserForm.phone,
      permissionNumber: this.newUserForm.permissionNumber,
      role: this.newUserForm.role
    };

    this.userService.createUser(newUser).subscribe(
      (response) => {
        console.log('[Settings] User created:', response);
        this.notificationService.success('Uzytkownik zostal pomyslnie utworzony!');
        this.creatingUser = false;
        this.resetCreateUserForm();
        this.showCreateUserForm = false;
        this.createUserDrawerOpen = false;

        // Reload users list
        this.loadUsers();
      },
      (error: any) => {
        console.error('[Settings] Error creating user:', error);
        const errorMsg = error.error?.error || error.error?.message || 'Blad podczas tworzenia uzytkownika';
        this.notificationService.error(errorMsg);
        this.creatingUser = false;
      }
    );
  }

  editUser(user: UserDTO): void {
    window.dispatchEvent(new CustomEvent('editUserEvent', { detail: user }));
  }

  canDeleteUser(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    const userId = user.id || user.userId;
    const currentUserId = currentUser.userId;

    // Cannot delete self
    if (currentUserId === userId) return false;

    // Only admins can delete users
    if (currentUser.role !== 'admin') return false;

    // Master admin (login = admin) can delete anyone
    if (currentUser.login === 'admin') return true;

    // Regular admin can only delete regular users (not admins)
    return user.role !== 'admin';
  }

  openDeleteConfirmDialog(user: UserDTO): void {
    console.log('[Settings] Opening delete dialog for user:', user);
    // Ask for password confirmation before deleting
    this.userToConfirm = user;
    this.confirmDialogAction = 'delete';
    this.showPasswordConfirmDialog = true;
  }

  confirmPasswordAction(): void {
    if (!this.confirmDialogPassword || !this.userToConfirm) {
      console.error('[Settings] Missing password or user to confirm');
      return;
    }

    // Use id or userId (backend returns 'id')
    const userId = this.userToConfirm.id || this.userToConfirm.userId;
    
    console.log('[Settings] Confirming delete for user:', this.userToConfirm);
    console.log('[Settings] User ID:', userId);

    if (!userId) {
      console.error('[Settings] User ID is undefined');
      this.notificationService.error('Blad: Brak ID uzytkownika');
      return;
    }

    this.confirmDialogError = '';
    this.confirmingAction = true;

    this.userService.deleteUser(userId, this.confirmDialogPassword).subscribe(
      (response) => {
        console.log('[Settings] User deleted:', response);
        this.showPasswordConfirmDialog = false;
        this.confirmingAction = false;
        this.confirmDialogPassword = '';
        this.userToConfirm = null;

        // Reload users list
        this.loadUsers();
        this.notificationService.success('Uzytkownik zostal pomyslnie usuniety!');
      },
      (error: any) => {
        console.error('[Settings] Error deleting user:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Blad podczas usuwania uzytkownika';
        this.notificationService.error(errorMsg);
        this.confirmingAction = false;
      }
    );
  }

  cancelPasswordConfirmDialog(): void {
    this.showPasswordConfirmDialog = false;
    this.confirmDialogPassword = '';
    this.confirmDialogError = '';
    this.userToConfirm = null;
  }

  canChangeUserRole(user: UserDTO): boolean {
    const currentUser = this.authService.getCurrentUser();
    if (!currentUser) return false;

    const userId = user.id || user.userId;
    const currentUserId = currentUser.userId;

    // Cannot change own role
    if (currentUserId === userId) return false;

    // Only master admin (login = admin) can change roles
    // Regular admin cannot change roles of existing users
    if (currentUser.login !== 'admin') return false;

    return true;
  }

  onRoleChanged(user: UserDTO): void {
    const currentUser = this.authService.getCurrentUser();
    
    // Validation - only master admin can change roles
    if (!currentUser || currentUser.login !== 'admin') {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Tylko master administrator moze zmieniac role uzytkowników');
      return;
    }

    // Validation - check if user can make this change
    if (!this.canChangeUserRole(user)) {
      // Reload user to reset the role
      this.loadUsers();
      this.notificationService.error('Brak uprawnien do zmiany tej roli');
      return;
    }

    const userId = user.id || user.userId;
    if (!userId) {
      this.notificationService.error('Blad: Brak ID uzytkownika');
      return;
    }

    this.updatingRoleUserId = userId;

    this.userService.updateUserRole(userId, user.role).subscribe(
      (response) => {
        console.log('[Settings] User role updated:', response);
        this.notificationService.success(`Rola uzytkownika ${user.login} zostala zmieniona`);
        this.updatingRoleUserId = null;
      },
      (error: any) => {
        console.error('[Settings] Error updating user role:', error);
        const errorMsg = error.error?.message || error.error?.error || 'Blad podczas zmiany roli uzytkownika';
        this.notificationService.error(errorMsg);
        this.updatingRoleUserId = null;
        // Reload users to revert the role change
        this.loadUsers();
      }
    );
  }

  // XSL templates stored per document type
  private xslByKey: Record<string, { name: string; uploadedAt: string; size: number }[]> = {};

  getXslTemplates(key: string) {
    return this.xslByKey[key] || [];
  }

  loadXslTemplates(): void {
    this.loadingXsl = true;
    this.http.get<{ templates: any[]; defaultTemplate: string }>('/api/settings/xsl-templates').subscribe({
      next: (res) => {
        // All templates shared across document types for now
        this.xslTemplates = res.templates;
        this.xslByKey['protocol'] = res.templates;
        this.xslByKey['register'] = res.templates;
        this.xslByKey['controlMarks'] = res.templates;
        this.loadingXsl = false;
      },
      error: () => { this.loadingXsl = false; }
    });
  }

  triggerXslUpload(key: string): void {
    const el = document.getElementById('xslInput-' + key) as HTMLInputElement;
    if (el) el.click();
  }

  onXslFileSelected(event: any, key: string = 'protocol'): void {
    const file: File = event.target.files[0];
    if (!file) return;
    if (!file.name.endsWith('.xsl')) { this.xslError = 'Dozwolone sa tylko pliki .xsl'; return; }
    this.uploadingXsl = true;
    this.xslError = '';
    const fd = new FormData();
    fd.append('file', file);
    this.http.post<{ name: string }>('/api/settings/xsl-templates/upload', fd).subscribe({
      next: () => { this.uploadingXsl = false; this.xslSuccess = 'Przeslano!'; this.loadXslTemplates(); setTimeout(() => this.xslSuccess = '', 3000); },
      error: (err) => { this.uploadingXsl = false; this.xslError = err.error?.message || 'Blad przesylania'; }
    });
  }

  deleteXsl(name: string, key: string = 'protocol'): void {
    if (!confirm(`Usunac szablon ${name}?`)) return;
    this.http.delete(`/api/settings/xsl-templates/${encodeURIComponent(name)}`).subscribe({
      next: () => { this.loadXslTemplates(); this.notificationService.success('Usunieto'); },
      error: () => { this.notificationService.error('Blad usuwania'); }
    });
  }

  // -- Session Management ------------------------------------------------------

  loadActiveSessions(): void {
    this.loadingSessions = true;
    this.http.get<any[]>('/api/sessions/active').subscribe({
      next: (sessions) => {
        this.activeSessions = sessions;
        this.loadingSessions = false;
      },
      error: (error) => {
        console.error('Failed to load sessions:', error);
        this.notificationService.error('Nie udalo sie zaladowac sesji');
        this.loadingSessions = false;
      }
    });
  }

  terminateSession(sessionId: number): void {
    if (!confirm('Czy na pewno chcesz zakonczyc te sesje?')) {
      return;
    }

    this.terminatingSessionId = sessionId;
    this.http.post(`/api/sessions/${sessionId}/terminate`, {}).subscribe({
      next: () => {
        this.notificationService.success('Sesja zostala zakonczona');
        this.loadActiveSessions();
        this.terminatingSessionId = null;
      },
      error: (error) => {
        console.error('Failed to terminate session:', error);
        this.notificationService.error('Nie udalo sie zakonczyc sesji');
        this.terminatingSessionId = null;
      }
    });
  }

  terminateAllUserSessions(userId: number, userLogin: string): void {
    if (!confirm(`Czy na pewno chcesz zakonczyc wszystkie sesje uzytkownika ${userLogin}?`)) {
      return;
    }

    this.http.post(`/api/sessions/user/${userId}/terminate-all`, {}).subscribe({
      next: () => {
        this.notificationService.success('Wszystkie sesje uzytkownika zostaly zakonczone');
        this.loadActiveSessions();
      },
      error: (error) => {
        console.error('Failed to terminate user sessions:', error);
        this.notificationService.error('Nie udalo sie zakonczyc sesji uzytkownika');
      }
    });
  }

  formatDuration(minutes: number): string {
    if (minutes < 60) {
      return `${minutes} min`;
    }
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return `${hours}h ${mins}min`;
  }

  // -- Activity Logs ------------------------------------------------------

  loadUserActivities(userId?: number): void {
    this.loadingActivities = true;
    
    let url = '/api/activitylogs/all';
    const params: any = { limit: 500 };
    
    if (userId) {
      url = `/api/activitylogs/user/${userId}`;
    }
    
    if (this.activityDateFrom) {
      params.from = this.activityDateFrom;
    }
    
    if (this.activityDateTo) {
      params.to = this.activityDateTo;
    }

    this.http.get<any[]>(url, { params }).subscribe({
      next: (activities) => {
        this.userActivities = activities;
        this.loadingActivities = false;
      },
      error: (error) => {
        console.error('Failed to load activities:', error);
        this.notificationService.error('Nie udalo sie zaladowac dziennika aktywnosci');
        this.loadingActivities = false;
      }
    });
  }

  getActivityTypeLabel(type: string): string {
    const labels: any = {
      'Login': 'Logowanie',
      'Logout': 'Wylogowanie',
      'Create': 'Utworzenie',
      'Update': 'Aktualizacja',
      'Delete': 'Usuniecie',
      'View': 'Podglad',
      'Export': 'Eksport',
      'Import': 'Import',
      'PasswordChange': 'Zmiana hasla',
      'SettingsChange': 'Zmiana ustawien',
      'FileUpload': 'Przeslanie pliku',
      'FileDownload': 'Pobranie pliku',
      'SessionTerminated': 'Zakonczenie sesji'
    };
    return labels[type] || type;
  }

  getSafeHtml(html: string): SafeHtml {
    return this.sanitizer.bypassSecurityTrustHtml(html);
  }
}
