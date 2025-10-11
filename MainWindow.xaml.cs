using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private bool _isTextChanged = false;

    public MainWindow()
    {
        InitializeComponent();
        MainTextBox.TextChanged += MainTextBox_TextChanged;
    }

    private void MainTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _isTextChanged = true;
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        MainTextBox.Clear();
        _isTextChanged = false;
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            MainTextBox.Text = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
            _isTextChanged = false;
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        if (saveFileDialog.ShowDialog() == true)
        {
            File.WriteAllText(saveFileDialog.FileName, MainTextBox.Text, Encoding.UTF8);
            _isTextChanged = false;
        }
    }

    // -----------------------------------------------------------------------------------

    // Define key and IV globally in your MainWindow class for simplicity. In a real application, secure key management is crucial.
    private static byte[] AesKey = Encoding.UTF8.GetBytes("This is a key123"); // Ensure your key is 16 bytes for AES-128, 24 for AES-192, or 32 for AES-256
    private static byte[] AesIV = Encoding.UTF8.GetBytes("This is an IV123"); // Ensure your IV is 16 bytes
    private static byte[] salt = Encoding.UTF8.GetBytes("A unique salt"); // Ensure this is securely managed and consistent for each user/document

    private void LoadEncrypted_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        if (openFileDialog.ShowDialog() == true)
        {
            PasswordDialog passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog() == true)
            {
                string password = passwordDialog.Password;

                int iterations = 10000; // Example iteration count, adjust based on performance and security needs
                byte[] keyAndIV = EncryptOperations.DeriveKeyAndIV(password, salt, 256, 128, iterations);
                byte[] key = keyAndIV.Take(32).ToArray();
                byte[] iv = keyAndIV.Skip(32).Take(16).ToArray();

                try
                {
                    byte[] encryptedContent = File.ReadAllBytes(openFileDialog.FileName);
                    string decryptedText = EncryptOperations.DecryptStringFromBytes_Aes(encryptedContent, key, iv);
                    MainTextBox.Text = decryptedText;
                    _isTextChanged = false;
                }
                catch (CryptographicException)
                {
                    MessageBox.Show("Failed to decrypt the file. It may not be encrypted or is corrupted.", "Decryption Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex) // General exception catch, in case of unexpected errors.
                {
                    MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }


    private void SaveEncrypted_Click(object sender, RoutedEventArgs e)
    {
        SaveFileDialog saveFileDialog = new SaveFileDialog();
        if (saveFileDialog.ShowDialog() == true)
        {
            PasswordDialog passwordDialog = new PasswordDialog();
            if (passwordDialog.ShowDialog() == true)
            {
                string password = passwordDialog.Password;

                int iterations = 10000; // Example iteration count, adjust based on performance and security needs
                byte[] keyAndIV = EncryptOperations.DeriveKeyAndIV(password, salt, 256, 128, iterations);
                byte[] key = keyAndIV.Take(32).ToArray();
                byte[] iv = keyAndIV.Skip(32).Take(16).ToArray();

                byte[] encryptedContent = EncryptOperations.EncryptStringToBytes_Aes(MainTextBox.Text, key, iv);
                File.WriteAllBytes(saveFileDialog.FileName, encryptedContent);
                _isTextChanged = false;
            }
        }
    }

    // -----------------------------------------------------------------------------------

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        if (_isTextChanged)
        {
            MessageBoxResult result = MessageBox.Show("You have unsaved changes. Do you want to save before exiting?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Save_Click(sender, e); // Attempt to save changes.

                // Check if text changed flag is still true, meaning save was cancelled.
                if (_isTextChanged)
                {
                    return; // Exit cancelled, return without shutting down.
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                return; // Exit cancelled, return without shutting down.
            }
        }

        Application.Current.Shutdown(); // Exit the application if no unsaved changes or if user chose not to save.
    }

    // -----------------------------------------------------------------------------------

    // on application exit, check for unsaved changes
    protected override void OnClosing(CancelEventArgs e)
    {
        if (_isTextChanged)
        {
            MessageBoxResult result = MessageBox.Show("You have unsaved changes. Do you want to save before exiting?", "Unsaved Changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Save_Click(this, new RoutedEventArgs()); // Attempt to save changes.

                // Check if text changed flag is still true, meaning save was cancelled.
                if (_isTextChanged)
                {
                    e.Cancel = true; // Exit cancelled, return without shutting down.
                    return;
                }
            }
            else if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true; // Exit cancelled, return without shutting down.
                return;
            }
        }

        base.OnClosing(e);
    }

}
